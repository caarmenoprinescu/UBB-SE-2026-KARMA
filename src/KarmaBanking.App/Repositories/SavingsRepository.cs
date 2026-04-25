// <copyright file="SavingsRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Models.Enums;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL-backed savings repository implementation.
/// </summary>
public class SavingsRepository : ISavingsRepository
{
    /// <summary>
    /// Gets savings accounts for a user with optional inclusion of closed accounts.
    /// </summary>
    /// <param name="userIdentificationNumber">The user identifier.</param>
    /// <param name="includesClosedAccounts">Whether closed accounts should be included.</param>
    /// <returns>The user's matching savings accounts.</returns>
    public async Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(
        int userIdentificationNumber,
        bool includesClosedAccounts = false)
    {
        var selectAccountsQuery = @"
                SELECT id, userId, savingsType, balance, accruedInterest, apy,
                       maturityDate, accountStatus, createdAt,
                       accountName, fundingAccountId, targetAmount, targetDate
                FROM SavingsAccount
                WHERE userId = @UserId"
                                  + (includesClosedAccounts ? string.Empty : " AND accountStatus != 'Closed'") +
                                  " ORDER BY balance DESC";

        var accountsList = new List<SavingsAccount>();

        using var dbConnection = DatabaseConfig.GetDatabaseConnection();
        await dbConnection.OpenAsync();

        using var sqlCommand = new SqlCommand(selectAccountsQuery, dbConnection);
        sqlCommand.Parameters.AddWithValue("@UserId", userIdentificationNumber);

        using var reader = await sqlCommand.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            accountsList.Add(MapReaderToAccount(reader));
        }

        return accountsList;
    }

    /// <summary>
    /// Creates a new savings account using the provided request and APY.
    /// </summary>
    /// <param name="dto">The create-account request payload.</param>
    /// <param name="apy">The annual percentage yield to assign.</param>
    /// <returns>The created savings account.</returns>
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

        using var dbConnection = DatabaseConfig.GetDatabaseConnection();
        await dbConnection.OpenAsync();

        using var sqlCommand = new SqlCommand(insertAccountQuery, dbConnection);
        sqlCommand.Parameters.AddWithValue("@UserId", dto.UserIdentificationNumber);
        sqlCommand.Parameters.AddWithValue("@SavingsType", dto.SavingsType);
        sqlCommand.Parameters.AddWithValue("@Balance", dto.InitialDeposit);
        sqlCommand.Parameters.AddWithValue("@Apy", apy);
        sqlCommand.Parameters.AddWithValue("@MaturityDate", (object?)dto.MaturityDate ?? DBNull.Value);
        sqlCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        sqlCommand.Parameters.AddWithValue("@AccountName", (object?)dto.AccountName ?? DBNull.Value);
        sqlCommand.Parameters.AddWithValue(
            "@FundingAccountId",
            dto.FundingAccountId == 0 ? DBNull.Value : dto.FundingAccountId);
        sqlCommand.Parameters.AddWithValue("@TargetAmount", (object?)dto.TargetAmount ?? DBNull.Value);
        sqlCommand.Parameters.AddWithValue("@TargetDate", (object?)dto.TargetDate ?? DBNull.Value);

        var newSavingsAccountIdentificationNumber = (int)(await sqlCommand.ExecuteScalarAsync())!;

        return new SavingsAccount
        {
            IdentificationNumber = newSavingsAccountIdentificationNumber,
            UserIdentificationNumber = dto.UserIdentificationNumber,
            SavingsType = dto.SavingsType,
            AccountName = dto.AccountName,
            Balance = dto.InitialDeposit,
            AccruedInterest = 0,
            Apy = apy,
            AccountStatus = "Active",
            CreatedAt = DateTime.Now,
            FundingAccountIdentificationNumber = dto.FundingAccountId == 0 ? null : dto.FundingAccountId,
            TargetAmount = dto.TargetAmount,
            TargetDate = dto.TargetDate,
        };
    }

    /// <summary>
    /// Deposits funds into a savings account and records a transaction row.
    /// </summary>
    /// <param name="accountIdentificationNumber">The target account identifier.</param>
    /// <param name="amount">The amount to deposit.</param>
    /// <param name="source">The source label for the deposit.</param>
    /// <returns>The resulting deposit response.</returns>
    public async Task<DepositResponseDto> DepositAsync(int accountIdentificationNumber, decimal amount, string source)
    {
        using var dbConnection = DatabaseConfig.GetDatabaseConnection();
        await dbConnection.OpenAsync();
        using var sqlTransaction = dbConnection.BeginTransaction();

        try
        {
            const string updateAccountBalanceQuery = @"
                    UPDATE SavingsAccount
                    SET balance = balance + @Amount
                    WHERE id = @AccountId";

            using var sqlUpdateAccountBalanceCommand = new SqlCommand(
                updateAccountBalanceQuery,
                dbConnection,
                sqlTransaction);
            sqlUpdateAccountBalanceCommand.Parameters.AddWithValue("@Amount", amount);
            sqlUpdateAccountBalanceCommand.Parameters.AddWithValue("@AccountId", accountIdentificationNumber);
            await sqlUpdateAccountBalanceCommand.ExecuteNonQueryAsync();

            decimal newAccountBalance;

            const string selectAccountBalanceQuery = "SELECT balance FROM SavingsAccount WHERE id = @AccountId";
            using (var sqlSelectAccountBalanceCommand = new SqlCommand(
                       selectAccountBalanceQuery,
                       dbConnection,
                       sqlTransaction))
            {
                sqlSelectAccountBalanceCommand.Parameters.AddWithValue("@AccountId", accountIdentificationNumber);
                newAccountBalance = (decimal)(await sqlSelectAccountBalanceCommand.ExecuteScalarAsync())!;
            }

            const string insertTransactionQuery = @"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                OUTPUT INSERTED.id
                VALUES (@AccountId, @TransactionType, @Amount, @BalanceAfter, @Source, @Description, GETUTCDATE())";

            using var sqlInsertTransactionCommand =
                new SqlCommand(insertTransactionQuery, dbConnection, sqlTransaction);

            sqlInsertTransactionCommand.Parameters.AddWithValue("@AccountId", accountIdentificationNumber);
            sqlInsertTransactionCommand.Parameters.AddWithValue("@TransactionType", "Deposit");
            sqlInsertTransactionCommand.Parameters.AddWithValue("@Amount", amount);
            sqlInsertTransactionCommand.Parameters.AddWithValue("@BalanceAfter", newAccountBalance);
            sqlInsertTransactionCommand.Parameters.AddWithValue("@Source", source ?? "Manual");
            sqlInsertTransactionCommand.Parameters.AddWithValue("@Description", DBNull.Value);

            var newTransactionIdentificationNumber = (int)(await sqlInsertTransactionCommand.ExecuteScalarAsync())!;

            await sqlTransaction.CommitAsync();

            return new DepositResponseDto
            {
                NewBalance = newAccountBalance,
                TransactionId = newTransactionIdentificationNumber,
                Timestamp = DateTime.Now,
            };
        }
        catch
        {
            await sqlTransaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Closes a savings account and transfers the specified amount to another account.
    /// </summary>
    /// <param name="accountIdentificationNumber">The source account identifier to close.</param>
    /// <param name="destinationAccountIdentificationNumber">The destination account identifier.</param>
    /// <param name="transferAmount">The amount to transfer out during closure.</param>
    /// <param name="earlyClosurePenalty">The penalty applied on closure, if any.</param>
    /// <returns>The closure operation result.</returns>
    public async Task<ClosureResultDto> CloseSavingsAccountAsync(
        int accountIdentificationNumber,
        int destinationAccountIdentificationNumber,
        decimal transferAmount,
        decimal earlyClosurePenalty)
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
            using (var selectSourceAccountDataCommand = new SqlCommand(
                       @"
                SELECT balance, savingsType, maturityDate, accountStatus
                FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
                WHERE id = @Id",
                       dbConnection,
                       dbTransaction))
            {
                selectSourceAccountDataCommand.Parameters.AddWithValue("@Id", accountIdentificationNumber);

                using var reader = await selectSourceAccountDataCommand.ExecuteReaderAsync();

                oldAccountBalance = (decimal)reader["balance"];
                oldAccountType = reader["savingsType"].ToString()!;
                oldAccountMaturityDate = reader["maturityDate"] as DateTime?;
            }

            // 2. TRANSFER TO DESTINATION
            using (var transferAmountToDestinationCommand = new SqlCommand(
                       @"
                UPDATE SavingsAccount 
                SET balance = balance + @Amount
                WHERE id = @DestId",
                       dbConnection,
                       dbTransaction))
            {
                transferAmountToDestinationCommand.Parameters.AddWithValue("@Amount", transferAmount);
                transferAmountToDestinationCommand.Parameters.AddWithValue("@DestId", destinationAccountIdentificationNumber);

                await transferAmountToDestinationCommand.ExecuteNonQueryAsync();
            }

            // 3. CLOSE ACCOUNT
            using (var closeAccountCommand = new SqlCommand(
                       @"
                UPDATE SavingsAccount
                SET balance = 0,
                    accountStatus = 'Closed',
                    updatedAt = GETUTCDATE()
                WHERE id = @Id",
                       dbConnection,
                       dbTransaction))
            {
                closeAccountCommand.Parameters.AddWithValue("@Id", accountIdentificationNumber);
                await closeAccountCommand.ExecuteNonQueryAsync();
            }

            // 4. INSERT CLOSURE TRANSACTION
            using (var insertClosureTransactionCommand = new SqlCommand(
                       @"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES
                (@AccountId, 'Closure', @Amount, 0, 'Closure', 'Account closed', GETUTCDATE())",
                       dbConnection,
                       dbTransaction))
            {
                insertClosureTransactionCommand.Parameters.AddWithValue("@AccountId", accountIdentificationNumber);
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
                ClosedAt = DateTime.UtcNow,
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
                ClosedAt = DateTime.UtcNow,
            };
        }
    }

    /// <summary>
    /// Withdraws funds from a savings account and logs the transaction.
    /// </summary>
    /// <param name="accountId">The source account identifier.</param>
    /// <param name="amount">The amount to withdraw.</param>
    /// <param name="destinationLabel">The destination label shown in transaction history.</param>
    /// <param name="earlyWithdrawalPenalty">The early-withdrawal penalty, if any.</param>
    /// <returns>The withdrawal operation result.</returns>
    public async Task<WithdrawResponseDto> WithdrawAsync(
        int accountId,
        decimal amount,
        string destinationLabel,
        decimal earlyWithdrawalPenalty)
    {
        using var dbConnection = DatabaseConfig.GetDatabaseConnection();
        await dbConnection.OpenAsync();
        using var dbTransaction = dbConnection.BeginTransaction();

        try
        {
            string savingsAccountType;
            DateTime? maturityDate;
            decimal oldBalance;

            using (var selectAccountDataCommand = new SqlCommand(
                       @"
                SELECT balance, savingsType, maturityDate
                FROM SavingsAccount WITH (UPDLOCK, ROWLOCK)
                WHERE id = @Id",
                       dbConnection,
                       dbTransaction))
            {
                selectAccountDataCommand.Parameters.AddWithValue("@Id", accountId);
                using var reader = await selectAccountDataCommand.ExecuteReaderAsync();

                oldBalance = (decimal)reader["balance"];
                savingsAccountType = reader["savingsType"].ToString()!;
                maturityDate = reader["maturityDate"] as DateTime?;
            }

            var newBalance = oldBalance - amount;

            using (var updateAccountBalanceCommand = new SqlCommand(
                       @"
                UPDATE SavingsAccount SET balance = @Balance WHERE id = @Id",
                       dbConnection,
                       dbTransaction))
            {
                updateAccountBalanceCommand.Parameters.AddWithValue("@Balance", newBalance);
                updateAccountBalanceCommand.Parameters.AddWithValue("@Id", accountId);
                await updateAccountBalanceCommand.ExecuteNonQueryAsync();
            }

            using (var insertWithdrawalTransactionCommand = new SqlCommand(
                       @"
                INSERT INTO SavingsTransaction
                (accountId, transactionType, amount, balanceAfter, source, description, createdAt)
                VALUES (@AccountId, 'Withdrawal', @Amount, @BalanceAfter, 'Manual',
                        @Description, GETUTCDATE())",
                       dbConnection,
                       dbTransaction))
            {
                insertWithdrawalTransactionCommand.Parameters.AddWithValue("@AccountId", accountId);
                insertWithdrawalTransactionCommand.Parameters.AddWithValue("@Amount", amount);
                insertWithdrawalTransactionCommand.Parameters.AddWithValue("@BalanceAfter", newBalance);

                var withdrawalDescription = earlyWithdrawalPenalty > 0
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
                ProcessedAt = DateTime.UtcNow,
            };
        }
        catch (Exception exception)
        {
            await dbTransaction.RollbackAsync();
            return new WithdrawResponseDto
            {
                Success = false,
                Message = exception.Message,
                ProcessedAt = DateTime.UtcNow,
            };
        }
    }

    /// <summary>
    /// Gets auto-deposit configuration for a savings account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <returns>The auto-deposit settings, or <see langword="null"/> when missing.</returns>
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

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AutoDeposit
        {
            Id = (int)reader["id"],
            SavingsAccountId = (int)reader["savingsAccountId"],
            Amount = (decimal)reader["amount"],
            Frequency = Enum.Parse<DepositFrequency>(reader["frequency"].ToString()!),
            NextRunDate = (DateTime)reader["nextRunDate"],
            IsActive = (bool)reader["isActive"],
        };
    }

    /// <summary>
    /// Creates or updates auto-deposit settings for a savings account.
    /// </summary>
    /// <param name="autoDeposit">The auto-deposit entity to save.</param>
    /// <returns>A task that completes when persistence is done.</returns>
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

    /// <summary>
    /// Gets available funding-source options for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The list of funding-source options.</returns>
    public Task<List<FundingSourceOption>> GetFundingSourcesAsync(int userId)
    {
        return Task.FromResult(
            new List<FundingSourceOption>
            {
                new() { Id = 1, DisplayName = "Checking Account ****1234" },
                new() { Id = 2, DisplayName = "Checking Account ****5678" },
            });
    }

    /// <summary>
    /// Gets paginated savings transactions for an account and filter.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="typeFilter">The transaction-type filter value.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A tuple containing page items and total transaction count.</returns>
    public async Task<(List<SavingsTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
        int accountId,
        string typeFilter,
        int page,
        int pageSize)
    {
        using var dbConnection = DatabaseConfig.GetDatabaseConnection();
        await dbConnection.OpenAsync();

        var baseQuery = @"
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
        {
            countAccountTransactionsCommand.Parameters.AddWithValue("@Type", typeFilter);
        }

        var numberOfAccountTransactions = (int)(await countAccountTransactionsCommand.ExecuteScalarAsync())!;

        // paginated selectAccountsQuery
        var paginatedSelectAccountsQuery = @"
                SELECT id, accountId, transactionType, amount, balanceAfter, source, description, createdAt
                " + baseQuery + @"
                ORDER BY createdAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        using var paginatedSelectAccountsCommand = new SqlCommand(paginatedSelectAccountsQuery, dbConnection);
        paginatedSelectAccountsCommand.Parameters.AddWithValue("@AccountId", accountId);
        paginatedSelectAccountsCommand.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        paginatedSelectAccountsCommand.Parameters.AddWithValue("@PageSize", pageSize);

        if (baseQuery.Contains("@Type"))
        {
            paginatedSelectAccountsCommand.Parameters.AddWithValue("@Type", typeFilter);
        }

        var transactionsList = new List<SavingsTransaction>();

        using var reader = await paginatedSelectAccountsCommand.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            transactionsList.Add(
                new SavingsTransaction
                {
                    IdentificationNumber = (int)reader["id"],
                    AccountIdentificationNumber = (int)reader["accountId"],
                    Type = Enum.Parse<TransactionType>(reader["transactionType"].ToString()!),
                    Amount = (decimal)reader["amount"],
                    BalanceAfter = (decimal)reader["balanceAfter"],
                    Source = reader["source"].ToString(),
                    Description = reader["description"] as string,
                    CreatedAt = (DateTime)reader["createdAt"],
                });
        }

        return (transactionsList, numberOfAccountTransactions);
    }

    private static SavingsAccount MapReaderToAccount(SqlDataReader r)
    {
        return new SavingsAccount
        {
            IdentificationNumber = r.GetInt32(r.GetOrdinal("id")),
            UserIdentificationNumber = r.GetInt32(r.GetOrdinal("userId")),
            SavingsType = r["savingsType"]?.ToString() ?? string.Empty,
            Balance = r.GetDecimal(r.GetOrdinal("balance")),
            AccruedInterest = r.GetDecimal(r.GetOrdinal("accruedInterest")),
            Apy = r.GetDecimal(r.GetOrdinal("apy")),
            MaturityDate = r["maturityDate"] as DateTime?,
            AccountStatus = r["accountStatus"]?.ToString() ?? string.Empty,
            CreatedAt = r.GetDateTime(r.GetOrdinal("createdAt")),
            AccountName = r["accountName"] as string,
            FundingAccountIdentificationNumber = r["fundingAccountId"] as int?,
            TargetAmount = r["targetAmount"] as decimal?,
            TargetDate = r["targetDate"] as DateTime?,
        };
    }
}