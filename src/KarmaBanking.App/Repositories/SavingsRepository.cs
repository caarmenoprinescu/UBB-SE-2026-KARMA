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
        public async Task<List<SavingsAccount>> GetByUserIdAsync(int userId, bool includesClosed = false)
        {
            string query = @"
                SELECT id, userId, savingsType, balance, accruedInterest, apy,
                       maturityDate, accountStatus, createdAt,
                       accountName, fundingAccountId, targetAmount, targetDate
                FROM SavingsAccount
                WHERE userId = @UserId"
                + (includesClosed ? "" : " AND accountStatus != 'Closed'") +
                " ORDER BY balance DESC";

            var accounts = new List<SavingsAccount>();

            using SqlConnection conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                accounts.Add(MapReaderToAccount(reader));

            return accounts;
        }

        public async Task<SavingsAccount> CreateAsync(CreateSavingsAccountDto dto)
        {
            decimal apy = dto.SavingsType switch
            {
                "FixedDeposit" => 0.04m,
                "GoalSavings"  => 0.03m,
                "HighYield"    => 0.03m,
                _              => 0.02m
            };

            const string query = @"
                INSERT INTO SavingsAccount
                    (userId, savingsType, balance, accruedInterest, apy, maturityDate,
                     accountStatus, createdAt, accountName,
                     fundingAccountId, targetAmount, targetDate)
                OUTPUT INSERTED.id
                VALUES
                    (@UserId, @SavingsType, @Balance, 0, @Apy, @MaturityDate,
                     'Active', @CreatedAt, @AccountName,
                     @FundingAccountId, @TargetAmount, @TargetDate)";

            using SqlConnection conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", dto.UserId);
            cmd.Parameters.AddWithValue("@SavingsType", dto.SavingsType);
            cmd.Parameters.AddWithValue("@Balance", dto.InitialDeposit);
            cmd.Parameters.AddWithValue("@Apy", apy);
            cmd.Parameters.AddWithValue("@MaturityDate", (object?)dto.MaturityDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
            cmd.Parameters.AddWithValue("@AccountName", (object?)dto.AccountName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FundingAccountId", dto.FundingAccountId == 0 ? (object)DBNull.Value : dto.FundingAccountId);
            cmd.Parameters.AddWithValue("@TargetAmount", (object?)dto.TargetAmount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TargetDate", (object?)dto.TargetDate ?? DBNull.Value);
            

            int newId = (int)await cmd.ExecuteScalarAsync();

            return new SavingsAccount
            {
                Id = newId,
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
            using SqlConnection conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();
            using SqlTransaction transaction = conn.BeginTransaction();

            try
            {
                const string updateQuery = @"
                    UPDATE SavingsAccount
                    SET balance = balance + @Amount
                    WHERE id = @AccountId";


                using SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction);
                updateCmd.Parameters.AddWithValue("@Amount", amount);
                updateCmd.Parameters.AddWithValue("@AccountId", accountId);
                await updateCmd.ExecuteNonQueryAsync();

                decimal newBalance;

                const string balanceQuery = "SELECT balance FROM SavingsAccount WHERE id = @AccountId";
                using (SqlCommand balCmd = new SqlCommand(balanceQuery, conn, transaction))
                {
                    balCmd.Parameters.AddWithValue("@AccountId", accountId);
                    newBalance = (decimal)await balCmd.ExecuteScalarAsync();
                }

                const string insertTxQuery = @"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                OUTPUT INSERTED.id
                VALUES (@AccountId, @TransactionType, @Amount, @BalanceAfter, @Source, @Description, GETUTCDATE())";

                using SqlCommand insertCmd = new SqlCommand(insertTxQuery, conn, transaction);

                insertCmd.Parameters.AddWithValue("@AccountId", accountId);
                insertCmd.Parameters.AddWithValue("@TransactionType", "Deposit");
                insertCmd.Parameters.AddWithValue("@Amount", amount);
                insertCmd.Parameters.AddWithValue("@BalanceAfter", newBalance);
                insertCmd.Parameters.AddWithValue("@Source", source ?? "Manual");
                insertCmd.Parameters.AddWithValue("@Description", DBNull.Value);

                int txId = (int)await insertCmd.ExecuteScalarAsync();


                await transaction.CommitAsync();

                return new DepositResponseDto
                {
                    NewBalance = newBalance,
                    TransactionId = txId,
                    Timestamp = DateTime.Now
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ClosureResult> CloseAsync(int accountId, int destinationAccountId)
        {
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                decimal balance;
                string accountType;
                DateTime? maturityDate;

                // 1. LOCK + FETCH ACCOUNT
                using (var cmd = new SqlCommand(@"
            SELECT balance, savingsType, maturityDate, accountStatus
            FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
            WHERE id = @Id", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException("Account not found.");

                    if (reader["accountStatus"].ToString() == "Closed")
                        throw new InvalidOperationException("Account already closed.");

                    balance = (decimal)reader["balance"];
                    accountType = reader["savingsType"].ToString();
                    maturityDate = reader["maturityDate"] as DateTime?;
                }

                // 2. VALIDATE DESTINATION ACCOUNT
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(1)
            FROM SavingsAccount
            WHERE id = @DestId", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@DestId", destinationAccountId);

                    int exists = (int)await cmd.ExecuteScalarAsync();

                    if (exists == 0)
                        throw new InvalidOperationException("Destination account not found.");
                }

                // 3. CALCULATE PENALTY
                decimal penalty = 0;

                if (accountType == "FixedDeposit" &&
                    maturityDate.HasValue &&
                    maturityDate > DateTime.UtcNow)
                {
                    penalty = balance * 0.02m;
                }

                decimal transferAmount = balance - penalty;

                // 4. TRANSFER TO DESTINATION
                using (var cmd = new SqlCommand(@"
            UPDATE SavingsAccount
            SET balance = balance + @Amount
            WHERE id = @DestId", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Amount", transferAmount);
                    cmd.Parameters.AddWithValue("@DestId", destinationAccountId);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. CLOSE ACCOUNT
                using (var cmd = new SqlCommand(@"
            UPDATE SavingsAccount
            SET balance = 0,
                accountStatus = 'Closed',
                updatedAt = GETUTCDATE()
            WHERE id = @Id", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 6. INSERT CLOSURE TRANSACTION
                using (var cmd = new SqlCommand(@"
            INSERT INTO SavingsTransaction
            (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
            VALUES
            (@AccountId, 'Closure', @Amount, 0, 'Closure', 'Account closed', GETUTCDATE())",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    cmd.Parameters.AddWithValue("@Amount", transferAmount);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new ClosureResult
                {
                    Success = true,
                    TransferredAmount = transferAmount,
                    PenaltyApplied = penalty,
                    Message = "Account closed successfully.",
                    ClosedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return new ClosureResult
                {
                    Success = false,
                    TransferredAmount = 0,
                    PenaltyApplied = 0,
                    Message = ex.Message,
                    ClosedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<WithdrawResponseDto> WithdrawAsync(int accountId, decimal amount, string destinationLabel)
        {
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();
            using var transaction = conn.BeginTransaction();

            try
            {
                string savingsType;
                DateTime? maturityDate;
                decimal balance;

                using (var cmd = new SqlCommand(@"
                    SELECT balance, savingsType, maturityDate
                    FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
                    WHERE id = @Id", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException("Account not found.");
                    balance = (decimal)reader["balance"];
                    savingsType = reader["savingsType"].ToString();
                    maturityDate = reader["maturityDate"] as DateTime?;
                }

                decimal penalty = 0;
                if (savingsType == "FixedDeposit" &&
                    maturityDate.HasValue &&
                    maturityDate.Value > DateTime.UtcNow)
                {
                    penalty = amount * 0.02m;
                }

                decimal totalDeducted = amount + penalty;
                if (totalDeducted > balance)
                    throw new InvalidOperationException("Insufficient balance after penalty.");

                decimal newBalance = balance - totalDeducted;

                using (var cmd = new SqlCommand(@"
                    UPDATE SavingsAccount SET balance = @Balance WHERE id = @Id",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Balance", newBalance);
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }

                using (var cmd = new SqlCommand(@"
                    INSERT INTO SavingsTransaction
                    (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                    VALUES (@AccountId, 'Withdrawal', @Amount, @BalanceAfter, 'Manual',
                            @Description, GETUTCDATE())", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    cmd.Parameters.AddWithValue("@Amount", amount);
                    cmd.Parameters.AddWithValue("@BalanceAfter", newBalance);
                    string desc = penalty > 0
                        ? $"To: {destinationLabel} | Early withdrawal penalty: {penalty:C2}"
                        : $"To: {destinationLabel}";
                    cmd.Parameters.AddWithValue("@Description", desc);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new WithdrawResponseDto
                {
                    Success = true,
                    AmountWithdrawn = amount,
                    PenaltyApplied = penalty,
                    NewBalance = newBalance,
                    Message = penalty > 0
                        ? $"Withdrawal successful. Early penalty of {penalty:C2} applied."
                        : "Withdrawal successful.",
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new WithdrawResponseDto
                {
                    Success = false,
                    Message = ex.Message,
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<AutoDeposit?> GetAutoDepositAsync(int accountId)
        {
            const string query = @"
                SELECT id, savingsAccountId, amount, frequency, nextRunDate, isActive
                FROM AutoDeposit
                WHERE savingsAccountId = @AccountId";

            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            using var reader = await cmd.ExecuteReaderAsync();

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
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            if (autoDeposit.Id == 0)
            {
                const string insert = @"
                    INSERT INTO AutoDeposit (savingsAccountId, amount, frequency, nextRunDate, isActive)
                    VALUES (@AccountId, @Amount, @Frequency, @NextRunDate, @IsActive)";
                using var cmd = new SqlCommand(insert, conn);
                cmd.Parameters.AddWithValue("@AccountId", autoDeposit.SavingsAccountId);
                cmd.Parameters.AddWithValue("@Amount", autoDeposit.Amount);
                cmd.Parameters.AddWithValue("@Frequency", autoDeposit.Frequency.ToString());
                cmd.Parameters.AddWithValue("@NextRunDate", autoDeposit.NextRunDate);
                cmd.Parameters.AddWithValue("@IsActive", autoDeposit.IsActive);
                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                const string update = @"
                    UPDATE AutoDeposit
                    SET amount = @Amount, frequency = @Frequency,
                        nextRunDate = @NextRunDate, isActive = @IsActive
                    WHERE id = @Id";
                using var cmd = new SqlCommand(update, conn);
                cmd.Parameters.AddWithValue("@Id", autoDeposit.Id);
                cmd.Parameters.AddWithValue("@Amount", autoDeposit.Amount);
                cmd.Parameters.AddWithValue("@Frequency", autoDeposit.Frequency.ToString());
                cmd.Parameters.AddWithValue("@NextRunDate", autoDeposit.NextRunDate);
                cmd.Parameters.AddWithValue("@IsActive", autoDeposit.IsActive);
                await cmd.ExecuteNonQueryAsync();
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

                // 3. Insert transaction
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
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            string baseQuery = @"
        FROM SavingsTransaction
        WHERE accountId = @AccountId";

            // filter
            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
            {
                baseQuery += " AND transactionType = @Type";
            }

            // total count
            using var countCmd = new SqlCommand("SELECT COUNT(*) " + baseQuery, conn);
            countCmd.Parameters.AddWithValue("@AccountId", accountId);

            if (baseQuery.Contains("@Type"))
                countCmd.Parameters.AddWithValue("@Type", typeFilter);

            int totalCount = (int)await countCmd.ExecuteScalarAsync();

            // paginated query
            string query = @"
        SELECT id, accountId, transactionType, amount, balanceAfter, source, description, createdAt
        " + baseQuery + @"
        ORDER BY createdAt DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@AccountId", accountId);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@PageSize", pageSize);

            if (baseQuery.Contains("@Type"))
                cmd.Parameters.AddWithValue("@Type", typeFilter);

            var list = new List<SavingsTransaction>();

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

            return (list, totalCount);
        }
    }
}
