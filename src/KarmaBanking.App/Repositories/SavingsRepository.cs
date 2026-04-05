using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Models.Enums;
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
                    (userId, savingsType, balance, accruedInterest, apy,
                     accountStatus, createdAt, accountName,
                     fundingAccountId, targetAmount, targetDate)
                OUTPUT INSERTED.id
                VALUES
                    (@UserId, @SavingsType, @Balance, 0, @Apy,
                     'Active', @CreatedAt, @AccountName,
                     @FundingAccountId, @TargetAmount, @TargetDate)";

            using SqlConnection conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", dto.UserId);
            cmd.Parameters.AddWithValue("@SavingsType", dto.SavingsType);
            cmd.Parameters.AddWithValue("@Balance", dto.InitialDeposit);
            cmd.Parameters.AddWithValue("@Apy", apy);
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

        public async Task<bool> CloseAsync(int accountId, int destinationAccountId)
        {
            using var conn = DatabaseConfig.GetDatabaseConnection();
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                decimal balance;
                string accountType;
                DateTime? maturityDate;

                // 1. Get account details
                using (var cmd = new SqlCommand(
                    "SELECT balance, savingsType, maturityDate, accountStatus FROM SavingsAccount WHERE id = @Id",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return false;

                    if (reader["accountStatus"].ToString() == "Closed")
                        throw new InvalidOperationException("Account already closed.");

                    balance = (decimal)reader["balance"];
                    accountType = reader["savingsType"].ToString();
                    maturityDate = reader["maturityDate"] as DateTime?;
                }

                // 2. Calculate penalty
                decimal penalty = 0;

                if (accountType == "FixedDeposit" &&
                    maturityDate.HasValue &&
                    maturityDate > DateTime.UtcNow)
                {
                    penalty = balance * 0.02m;
                }

                decimal transferAmount = balance - penalty;

                // 3. Transfer to destination (simplified: just add to balance)
                using (var cmd = new SqlCommand(
                    "UPDATE SavingsAccount SET balance = balance + @Amount WHERE id = @DestId",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Amount", transferAmount);
                    cmd.Parameters.AddWithValue("@DestId", destinationAccountId);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Close original account
                using (var cmd = new SqlCommand(
                    "UPDATE SavingsAccount SET balance = 0, accountStatus = 'Closed', updatedAt = GETUTCDATE() WHERE id = @Id",
                    conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", accountId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. Insert closure transaction
                using (var cmd = new SqlCommand(@"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES
                (@AccountId, @Type, @Amount, 0, @Source, @Description, GETUTCDATE())",
                conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    cmd.Parameters.AddWithValue("@Type", "Closure");
                    cmd.Parameters.AddWithValue("@Amount", transferAmount);
                    cmd.Parameters.AddWithValue("@Source", "Closure");
                    cmd.Parameters.AddWithValue("@Description", "Account closed");

                    await cmd.ExecuteNonQueryAsync();
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
