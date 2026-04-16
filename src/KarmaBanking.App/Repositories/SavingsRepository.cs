using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Models.Enums;
using System.Linq;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KarmaBanking.App.Repositories
{
    public class SavingsRepository : ISavingsRepository
    {
        public SavingsRepository() { }
        
        public async Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId, bool includesClosedAccounts = false)
        {
            string selectAccountsQuery = @"
                SELECT id, userId, savingsType, balance, accruedInterest, apy,
                       maturityDate, accountStatus, createdAt,
                       accountName, fundingAccountId, targetAmount, targetDate
                FROM SavingsAccount
                WHERE userId = @UserId"
                + (includesClosedAccounts ? "" : " AND accountStatus != 'Closed'") +
                " ORDER BY balance DESC";

            var accountsList = new List<SavingsAccount>();
                
            using SqlConnection dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            using SqlCommand sqlCommand = new SqlCommand(selectAccountsQuery, dbConnection);
            sqlCommand.Parameters.AddWithValue("@UserId", userId);

            using SqlDataReader reader = await sqlCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                accountsList.Add(MapReaderToAccount(reader));

            return accountsList;
        }


        public async Task<SavingsAccount> CreateSavingsAccountAsync(CreateSavingsAccountDto dto, decimal apy)
        {
            const string insertAccountQuery = @"
                INSERT INTO SavingsAccount
                    (userId, savingsType, balance, accruedInterest, apy, maturityDate,
                     accountStatus, createdAt, accountName,
                     fundingAccountId, targetAmount, targetDate)
                OUTPUT INSERTED.id
                VALUES
                    (@UserId, @SavingsType, @Balance, 0, @Apy, @MaturityDate,
                     'Active', @CreatedAt, @AccountName,
                     @FundingAccountId, @TargetAmount, @TargetDate)";

            using SqlConnection dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            using SqlCommand sqlCommand = new SqlCommand(insertAccountQuery, dbConnection);
            sqlCommand.Parameters.AddWithValue("@UserId", dto.UserId);
            sqlCommand.Parameters.AddWithValue("@SavingsType", dto.SavingsType);
            sqlCommand.Parameters.AddWithValue("@Balance", dto.InitialDeposit);
            sqlCommand.Parameters.AddWithValue("@Apy", apy);
            sqlCommand.Parameters.AddWithValue("@MaturityDate", (object?)dto.MaturityDate ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            sqlCommand.Parameters.AddWithValue("@AccountName", (object?)dto.AccountName ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@FundingAccountId", dto.FundingAccountId == 0 ? (object)DBNull.Value : dto.FundingAccountId);
            sqlCommand.Parameters.AddWithValue("@TargetAmount", (object?)dto.TargetAmount ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue("@TargetDate", (object?)dto.TargetDate ?? DBNull.Value);
            
            int newSavingsAccountId = (int)await sqlCommand.ExecuteScalarAsync();

            return new SavingsAccount
            {
                Id = newSavingsAccountId,
                UserId = dto.UserId,
                SavingsType = dto.SavingsType,
                AccountName = dto.AccountName,
                Balance = dto.InitialDeposit,
                AccruedInterest = 0,
                Apy = apy,
                AccountStatus = "Active",
                CreatedAt = DateTime.Now,
                FundingAccountId = dto.FundingAccountId == 0 ? (int?)null : dto.FundingAccountId,
                TargetAmount = dto.TargetAmount,
                TargetDate = dto.TargetDate
            };
        }


        public async Task<DepositResponseDto> DepositAsync(int accountId, decimal amount, string source)
        {
            using SqlConnection dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();
            using SqlTransaction sqlTransaction = dbConnection.BeginTransaction();

            try
            {
                const string updateAccountBalanceQuery = @"
                    UPDATE SavingsAccount
                    SET balance = balance + @Amount
                    WHERE id = @AccountId";

                using SqlCommand sqlUpdateAccountBalanceCommand = new SqlCommand(updateAccountBalanceQuery, dbConnection, sqlTransaction);
                sqlUpdateAccountBalanceCommand.Parameters.AddWithValue("@Amount", amount);
                sqlUpdateAccountBalanceCommand.Parameters.AddWithValue("@AccountId", accountId);
                await sqlUpdateAccountBalanceCommand.ExecuteNonQueryAsync();

                decimal newAccountBalance;

                const string selectAccountBalanceQuery = "SELECT balance FROM SavingsAccount WHERE id = @AccountId";
                using (SqlCommand sqlSelectAccountBalanceCommand = new SqlCommand(selectAccountBalanceQuery, dbConnection, sqlTransaction))
                {
                    sqlSelectAccountBalanceCommand.Parameters.AddWithValue("@AccountId", accountId);
                    newAccountBalance = (decimal)await sqlSelectAccountBalanceCommand.ExecuteScalarAsync();
                }

                const string insertTransactionQuery = @"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                OUTPUT INSERTED.id
                VALUES (@AccountId, @TransactionType, @Amount, @BalanceAfter, @Source, @Description, GETUTCDATE())";

                using SqlCommand sqlInsertTransactionCommand = new SqlCommand(insertTransactionQuery, dbConnection, sqlTransaction);

                sqlInsertTransactionCommand.Parameters.AddWithValue("@AccountId", accountId);
                sqlInsertTransactionCommand.Parameters.AddWithValue("@TransactionType", "Deposit");
                sqlInsertTransactionCommand.Parameters.AddWithValue("@Amount", amount);
                sqlInsertTransactionCommand.Parameters.AddWithValue("@BalanceAfter", newAccountBalance);
                sqlInsertTransactionCommand.Parameters.AddWithValue("@Source", source ?? "Manual");
                sqlInsertTransactionCommand.Parameters.AddWithValue("@Description", DBNull.Value);

                int newTransactionId = (int)await sqlInsertTransactionCommand.ExecuteScalarAsync();

                await sqlTransaction.CommitAsync();

                return new DepositResponseDto
                {
                    NewBalance = newAccountBalance,
                    TransactionId = newTransactionId,
                    Timestamp = DateTime.Now
                };
            }
            catch
            {
                await sqlTransaction.RollbackAsync();
                throw;
            }
        }


        public async Task<ClosureResultDto> CloseSavingsAccountAsync(int accountId, int destinationAccountId, decimal transferAmount, decimal earlyClosurePenalty)
        {
            using var dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            using var dbTransaction = dbConnection.BeginTransaction();

            try
            {
                decimal oldAccountBalance;
                string oldAccountType;
                DateTime? oldAccountMaturityDate;

                // 1. LOCK + FETCH ACCOUNT
                using (var selectSourceAccountDataCommand = new SqlCommand(@"
                SELECT balance, savingsType, maturityDate, accountStatus
                FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
                WHERE id = @Id", dbConnection, dbTransaction))
                {
                    selectSourceAccountDataCommand.Parameters.AddWithValue("@Id", accountId);

                    using var reader = await selectSourceAccountDataCommand.ExecuteReaderAsync();

                    oldAccountBalance = (decimal)reader["balance"];
                    oldAccountType = reader["savingsType"].ToString();
                    oldAccountMaturityDate = reader["maturityDate"] as DateTime?;
                }

                // 2. TRANSFER TO DESTINATION
                using (var transferAmountToDestinationCommand = new SqlCommand(@"
                UPDATE SavingsAccount 
                SET balance = balance + @Amount
                WHERE id = @DestId", dbConnection, dbTransaction))
                {
                    transferAmountToDestinationCommand.Parameters.AddWithValue("@Amount", transferAmount);
                    transferAmountToDestinationCommand.Parameters.AddWithValue("@DestId", destinationAccountId);

                    await transferAmountToDestinationCommand.ExecuteNonQueryAsync();
                }

                // 3. CLOSE ACCOUNT
                using (var closeAccountCommand = new SqlCommand(@"
                UPDATE SavingsAccount
                SET balance = 0,
                    accountStatus = 'Closed',
                    updatedAt = GETUTCDATE()
                WHERE id = @Id", dbConnection, dbTransaction))
                {
                    closeAccountCommand.Parameters.AddWithValue("@Id", accountId);
                    await closeAccountCommand.ExecuteNonQueryAsync();
                }

                // 4. INSERT CLOSURE TRANSACTION
                using (var insertClosureTransactionCommand = new SqlCommand(@"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES
                (@AccountId, 'Closure', @Amount, 0, 'Closure', 'Account closed', GETUTCDATE())",
                    dbConnection, dbTransaction))
                {
                    insertClosureTransactionCommand.Parameters.AddWithValue("@AccountId", accountId);
                    insertClosureTransactionCommand.Parameters.AddWithValue("@Amount", transferAmount);

                    await insertClosureTransactionCommand.ExecuteNonQueryAsync();
                }

                await dbTransaction.CommitAsync();

                return new ClosureResultDto
                {
                    Success = true,
                    TransferredAmount = transferAmount,
                    PenaltyApplied = earlyClosurePenalty,
                    Message = "Account closed successfully.",
                    ClosedAt = DateTime.UtcNow
                };
            }
            catch (Exception exception)
            {
                await dbTransaction.RollbackAsync();

                return new ClosureResultDto
                {
                    Success = false,
                    TransferredAmount = 0,
                    PenaltyApplied = 0,
                    Message = exception.Message,
                    ClosedAt = DateTime.UtcNow
                };
            }
        }


        public async Task<WithdrawResponseDto> WithdrawAsync(int accountId, decimal amount, string destinationLabel, decimal earlyWithdrawalPenalty)
        {
            using var dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();
            using var dbTransaction = dbConnection.BeginTransaction();

            try
            {
                string savingsAccountType;
                DateTime? maturityDate;
                decimal oldBalance;

                using (var selectAccountDataCommand = new SqlCommand(@"
                SELECT balance, savingsType, maturityDate
                FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
                WHERE id = @Id", dbConnection, dbTransaction))
                {
                    selectAccountDataCommand.Parameters.AddWithValue("@Id", accountId);
                    using var reader = await selectAccountDataCommand.ExecuteReaderAsync();
                    
                    oldBalance = (decimal)reader["balance"];
                    savingsAccountType = reader["savingsType"].ToString();
                    maturityDate = reader["maturityDate"] as DateTime?;
                }

                decimal newBalance = oldBalance - amount;

                using (var updateAccountBalanceCommand = new SqlCommand(@"
                UPDATE SavingsAccount SET balance = @Balance WHERE id = @Id",
                dbConnection, dbTransaction))
                {
                    updateAccountBalanceCommand.Parameters.AddWithValue("@Balance", newBalance);
                    updateAccountBalanceCommand.Parameters.AddWithValue("@Id", accountId);
                    await updateAccountBalanceCommand.ExecuteNonQueryAsync();
                }

                using (var insertWithdrawalTransactionCommand = new SqlCommand(@"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES (@AccountId, 'Withdrawal', @Amount, @BalanceAfter, 'Manual',
                        @Description, GETUTCDATE())", dbConnection, dbTransaction))
                {
                    insertWithdrawalTransactionCommand.Parameters.AddWithValue("@AccountId", accountId);
                    insertWithdrawalTransactionCommand.Parameters.AddWithValue("@Amount", amount);
                    insertWithdrawalTransactionCommand.Parameters.AddWithValue("@BalanceAfter", newBalance);

                    string withdrawalDescription = earlyWithdrawalPenalty > 0
                        ? $"To: {destinationLabel} | Early withdrawal penalty: {earlyWithdrawalPenalty:C2}"
                        : $"To: {destinationLabel}";

                    insertWithdrawalTransactionCommand.Parameters.AddWithValue("@Description", withdrawalDescription);
                    await insertWithdrawalTransactionCommand.ExecuteNonQueryAsync();
                }

                await dbTransaction.CommitAsync();

                return new WithdrawResponseDto
                {
                    Success = true,
                    AmountWithdrawn = amount,
                    PenaltyApplied = earlyWithdrawalPenalty,
                    NewBalance = newBalance,
                    Message = earlyWithdrawalPenalty > 0
                        ? $"Withdrawal successful. Early penalty of {earlyWithdrawalPenalty:C2} applied."
                        : "Withdrawal successful.",
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception exception)
            {
                await dbTransaction.RollbackAsync();
                return new WithdrawResponseDto
                {
                    Success = false,
                    Message = exception.Message,
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }


        public async Task<AutoDeposit?> GetAutoDepositAsync(int accountId)
        {
            const string selectAutoDepositByAccountIdQuery = @"
                SELECT id, savingsAccountId, amount, frequency, nextRunDate, isActive
                FROM AutoDeposit
                WHERE savingsAccountId = @AccountId";

            using var dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            using var selectAutoDepositByAccountIdCommand = new SqlCommand(selectAutoDepositByAccountIdQuery, dbConnection);
            selectAutoDepositByAccountIdCommand.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = await selectAutoDepositByAccountIdCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            return new AutoDeposit
            {
                Id = (int)reader["id"],
                SavingsAccountId = (int)reader["savingsAccountId"],
                Amount = (decimal)reader["amount"],
                Frequency = Enum.Parse<DepositFrequency>(reader["frequency"].ToString()!),
                NextRunDate = (DateTime)reader["nextRunDate"],
                IsActive = (bool)reader["isActive"]
            };
        }


        public async Task SaveAutoDepositAsync(AutoDeposit autoDeposit)
        {
            using var dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            if (autoDeposit.Id == 0)
            {
                const string insertAutoDepositQuery = @"
                    INSERT INTO AutoDeposit (savingsAccountId, amount, frequency, nextRunDate, isActive)
                    VALUES (@AccountId, @Amount, @Frequency, @NextRunDate, @IsActive)";

                using var insertAutoDepositCommand = new SqlCommand(insertAutoDepositQuery, dbConnection);
                insertAutoDepositCommand.Parameters.AddWithValue("@AccountId", autoDeposit.SavingsAccountId);
                insertAutoDepositCommand.Parameters.AddWithValue("@Amount", autoDeposit.Amount);
                insertAutoDepositCommand.Parameters.AddWithValue("@Frequency", autoDeposit.Frequency.ToString());
                insertAutoDepositCommand.Parameters.AddWithValue("@NextRunDate", autoDeposit.NextRunDate);
                insertAutoDepositCommand.Parameters.AddWithValue("@IsActive", autoDeposit.IsActive);
                await insertAutoDepositCommand.ExecuteNonQueryAsync();
            }
            else
            {
                const string updateAutoDepositQuery = @"
                    UPDATE AutoDeposit
                    SET amount = @Amount, frequency = @Frequency,
                        nextRunDate = @NextRunDate, isActive = @IsActive
                    WHERE id = @Id";

                using var updateAutoDepositCommand = new SqlCommand(updateAutoDepositQuery, dbConnection);
                updateAutoDepositCommand.Parameters.AddWithValue("@Id", autoDeposit.Id);
                updateAutoDepositCommand.Parameters.AddWithValue("@Amount", autoDeposit.Amount);
                updateAutoDepositCommand.Parameters.AddWithValue("@Frequency", autoDeposit.Frequency.ToString());
                updateAutoDepositCommand.Parameters.AddWithValue("@NextRunDate", autoDeposit.NextRunDate);
                updateAutoDepositCommand.Parameters.AddWithValue("@IsActive", autoDeposit.IsActive);
                await updateAutoDepositCommand.ExecuteNonQueryAsync();
            }
        }


        public Task<List<FundingSourceOption>> GetFundingSourcesAsync(int userId)
        {
            return Task.FromResult(new List<FundingSourceOption>
            {
                new FundingSourceOption { Id = 1, DisplayName = "Checking Account ****1234" },
                new FundingSourceOption { Id = 2, DisplayName = "Checking Account ****5678" }
            });
        }


        private static SavingsAccount MapReaderToAccount(SqlDataReader r)
        {
            return new SavingsAccount
            {
                Id               = r.GetInt32(r.GetOrdinal("id")),
                UserId           = r.GetInt32(r.GetOrdinal("userId")),
                SavingsType      = r["savingsType"]?.ToString() ?? string.Empty,
                Balance          = r.GetDecimal(r.GetOrdinal("balance")),
                AccruedInterest  = r.GetDecimal(r.GetOrdinal("accruedInterest")),
                Apy              = r.GetDecimal(r.GetOrdinal("apy")),
                MaturityDate     = r["maturityDate"] as DateTime?,
                AccountStatus    = r["accountStatus"]?.ToString() ?? string.Empty,
                CreatedAt        = r.GetDateTime(r.GetOrdinal("createdAt")),
                AccountName      = r["accountName"] as string,
                FundingAccountId = r["fundingAccountId"] as int?,
                TargetAmount     = r["targetAmount"] as decimal?,
                TargetDate       = r["targetDate"] as DateTime?
            };
        }



        // UNUSED METHOD
        public async Task<List<SavingsTransaction>> GetTransactionsAsync(int accountId)
        {
            const string query = @"
        SELECT id, accountId, transactionType, amount, balanceAfter, source, description, createdAt
        FROM SavingsTransaction
        WHERE accountId = @AccountId
        ORDER BY createdAt DESC";

            var list = new List<SavingsTransaction>();

            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new SavingsTransaction
                {
                    Id = (int)reader["id"],
                    AccountId = (int)reader["accountId"],
                    Type = Enum.Parse<TransactionType>(reader["transactionType"].ToString()),
                    Amount = (decimal)reader["amount"],
                    BalanceAfter = (decimal)reader["balanceAfter"],
                    Source = reader["source"].ToString(),
                    Description = reader["description"] as string,
                    CreatedAt = (DateTime)reader["createdAt"]
                });
            }

            return list;
        }


        // UNUSED METHOD
        public async Task<bool> HasInterestTransactionThisMonthAsync(int accountId)
        {
            const string query = @"
        SELECT COUNT(1)
        FROM SavingsTransaction
        WHERE accountId = @AccountId
        AND transactionType = 'Interest'
        AND MONTH(createdAt) = MONTH(GETUTCDATE())
        AND YEAR(createdAt) = YEAR(GETUTCDATE())";

            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);

            int count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }


        // UNUSED METHOD
        public async Task<bool> DepositWithTransactionAsync(int accountId, decimal amount)
        {
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Get current balance
                decimal currentBalance;

                using (var getCmd = new SqlCommand(
                    "SELECT balance FROM SavingsAccount WHERE id = @Id",
                    conn, transaction))
                {
                    getCmd.Parameters.AddWithValue("@Id", accountId);

                    var result = await getCmd.ExecuteScalarAsync();

                    if (result == null)
                        return false;

                    currentBalance = (decimal)result;
                }

                decimal newBalance = currentBalance + amount;

                // 2. Update balance
                using (var updateCmd = new SqlCommand(
                    "UPDATE SavingsAccount SET balance = @Balance, updatedAt = GETUTCDATE() WHERE id = @Id",
                    conn, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@Balance", newBalance);
                    updateCmd.Parameters.AddWithValue("@Id", accountId);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                // 3. Insert sqlTransaction
                using (var insertCmd = new SqlCommand(@"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES
                (@AccountId, @TransactionType, @Amount, @BalanceAfter, @Source, @Description, GETUTCDATE())",
                    conn, transaction))
                {
                    insertCmd.Parameters.AddWithValue("@AccountId", accountId);
                    insertCmd.Parameters.AddWithValue("@TransactionType", "Deposit");
                    insertCmd.Parameters.AddWithValue("@Amount", amount);
                    insertCmd.Parameters.AddWithValue("@BalanceAfter", newBalance);
                    insertCmd.Parameters.AddWithValue("@Source", "Manual");

                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



        public async Task<(List<SavingsTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
        int accountId,
        string typeFilter,
        int page,
        int pageSize)
        {
            using var dbConnection = DatabaseConfig.GetDatabaseConnection();
            await dbConnection.OpenAsync();

            string baseQuery = @"
                FROM SavingsTransaction
                WHERE accountId = @AccountId";

            // filter
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
            {
                baseQuery += " AND transactionType = @Type";
            }

            // total count
            using var countAccountTransactionsCommand = new SqlCommand("SELECT COUNT(*) " + baseQuery, dbConnection);
            countAccountTransactionsCommand.Parameters.AddWithValue("@AccountId", accountId);

            if (baseQuery.Contains("@Type"))
                countAccountTransactionsCommand.Parameters.AddWithValue("@Type", typeFilter);

            int numberOfAccountTransactions = (int)await countAccountTransactionsCommand.ExecuteScalarAsync();

            // paginated selectAccountsQuery
            string paginatedSelectAccountsQuery = @"
                SELECT id, accountId, transactionType, amount, balanceAfter, source, description, createdAt
                " + baseQuery + @"
                ORDER BY createdAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var paginatedSelectAccountsCommand = new SqlCommand(paginatedSelectAccountsQuery, dbConnection);
            paginatedSelectAccountsCommand.Parameters.AddWithValue("@AccountId", accountId);
            paginatedSelectAccountsCommand.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            paginatedSelectAccountsCommand.Parameters.AddWithValue("@PageSize", pageSize);

            if (baseQuery.Contains("@Type"))
                paginatedSelectAccountsCommand.Parameters.AddWithValue("@Type", typeFilter);

            var transactionsList = new List<SavingsTransaction>();

            using var reader = await paginatedSelectAccountsCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                transactionsList.Add(new SavingsTransaction
                {
                    Id = (int)reader["id"],
                    AccountId = (int)reader["accountId"],
                    Type = Enum.Parse<TransactionType>(reader["transactionType"].ToString()),
                    Amount = (decimal)reader["amount"],
                    BalanceAfter = (decimal)reader["balanceAfter"],
                    Source = reader["source"].ToString(),
                    Description = reader["description"] as string,
                    CreatedAt = (DateTime)reader["createdAt"]
                });
            }

            return (transactionsList, numberOfAccountTransactions);
        }
    }
}
