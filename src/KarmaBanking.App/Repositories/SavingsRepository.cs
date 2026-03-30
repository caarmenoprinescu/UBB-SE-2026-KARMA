using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace KarmaBanking.App.Repositories
{
    public class SavingsRepository : ISavingsRepository
    {
        public async Task<bool> AddSavingsAccountAsync(SavingsAccount newSavingsAccount)
        {
            const string insertSavingsAccountQuery = @"
                INSERT INTO SavingsAccount
                    (userId, savingsType, balance, accruedInterest, apy,
                     maturityDate, accountStatus, createdAt,
                     accountName, fundingAccountId, targetAmount, targetDate)
                VALUES
                    (@UserId, @SavingsType, @Balance, @AccruedInterest, @Apy,
                     @MaturityDate, @AccountStatus, @CreatedAt,
                     @AccountName, @FundingAccountId, @TargetAmount, @TargetDate)";

            using SqlConnection openDatabaseConnection = DatabaseConfig.GetDatabaseConnection();
            await openDatabaseConnection.OpenAsync();

            using SqlCommand insertSavingsAccountCommand = new SqlCommand(insertSavingsAccountQuery, openDatabaseConnection);
            insertSavingsAccountCommand.Parameters.AddWithValue("@UserId", newSavingsAccount.UserId);
            insertSavingsAccountCommand.Parameters.AddWithValue("@SavingsType", newSavingsAccount.SavingsType);
            insertSavingsAccountCommand.Parameters.AddWithValue("@Balance", newSavingsAccount.Balance);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AccruedInterest", newSavingsAccount.AccruedInterest);
            insertSavingsAccountCommand.Parameters.AddWithValue("@Apy", newSavingsAccount.Apy);
            insertSavingsAccountCommand.Parameters.AddWithValue("@MaturityDate", (object?)newSavingsAccount.MaturityDate ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AccountStatus", newSavingsAccount.AccountStatus);
            insertSavingsAccountCommand.Parameters.AddWithValue("@CreatedAt", newSavingsAccount.CreatedAt);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AccountName", (object?)newSavingsAccount.AccountName ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@FundingAccountId", (object?)newSavingsAccount.FundingAccountId ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@TargetAmount", (object?)newSavingsAccount.TargetAmount ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@TargetDate", (object?)newSavingsAccount.TargetDate ?? DBNull.Value);

            int numberOfRowsAffectedByInsert = await insertSavingsAccountCommand.ExecuteNonQueryAsync();
            return numberOfRowsAffectedByInsert > 0;
        }

        public async Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId)
        {
            const string selectSavingsAccountsByUserIdQuery = @"
                SELECT id, userId, savingsType, balance, accruedInterest, apy,
                       maturityDate, accountStatus, createdAt,
                       accountName, fundingAccountId, targetAmount, targetDate
                FROM SavingsAccount
                WHERE userId = @UserId";

            List<SavingsAccount> savingsAccounts = new List<SavingsAccount>();

            using SqlConnection openDatabaseConnection = DatabaseConfig.GetDatabaseConnection();
            await openDatabaseConnection.OpenAsync();

            using SqlCommand selectSavingsAccountsCommand = new SqlCommand(selectSavingsAccountsByUserIdQuery, openDatabaseConnection);
            selectSavingsAccountsCommand.Parameters.AddWithValue("@UserId", userId);

            using SqlDataReader savingsAccountReader = await selectSavingsAccountsCommand.ExecuteReaderAsync();
            while (await savingsAccountReader.ReadAsync())
            {
                SavingsAccount savingsAccount = new SavingsAccount
                {
                    Id = (int)savingsAccountReader["id"],
                    UserId = (int)savingsAccountReader["userId"],
                    SavingsType = savingsAccountReader["savingsType"].ToString(),
                    Balance = (decimal)savingsAccountReader["balance"],
                    AccruedInterest = (decimal)savingsAccountReader["accruedInterest"],
                    Apy = (decimal)savingsAccountReader["apy"],
                    MaturityDate = savingsAccountReader["maturityDate"] as DateTime?,
                    AccountStatus = savingsAccountReader["accountStatus"].ToString(),
                    CreatedAt = (DateTime)savingsAccountReader["createdAt"],
                    AccountName = savingsAccountReader["accountName"] as string,
                    FundingAccountId = savingsAccountReader["fundingAccountId"] as int?,
                    TargetAmount = savingsAccountReader["targetAmount"] as decimal?,
                    TargetDate = savingsAccountReader["targetDate"] as DateTime?
                };
                savingsAccounts.Add(savingsAccount);
            }

            return savingsAccounts;
        }

        public async Task<bool> UpdateSavingsAccountBalanceAsync(int savingsAccountId, decimal amountToAdd)
        {
            const string updateSavingsAccountBalanceQuery = @"
                UPDATE SavingsAccount
                SET balance = balance + @AmountToAdd
                WHERE id = @SavingsAccountId";

            using SqlConnection openDatabaseConnection = DatabaseConfig.GetDatabaseConnection();
            await openDatabaseConnection.OpenAsync();

            using SqlCommand updateSavingsAccountBalanceCommand = new SqlCommand(updateSavingsAccountBalanceQuery, openDatabaseConnection);
            updateSavingsAccountBalanceCommand.Parameters.AddWithValue("@SavingsAccountId", savingsAccountId);
            updateSavingsAccountBalanceCommand.Parameters.AddWithValue("@AmountToAdd", amountToAdd);

            int numberOfRowsAffectedByUpdate = await updateSavingsAccountBalanceCommand.ExecuteNonQueryAsync();
            return numberOfRowsAffectedByUpdate > 0;
        }

        public async Task<bool> CloseSavingsAccountAsync(int savingsAccountId)
        {
            const string closeSavingsAccountQuery = @"
                UPDATE SavingsAccount
                SET balance = 0,
                    accountStatus = 'Closed'
                WHERE id = @SavingsAccountId";

            using SqlConnection openDatabaseConnection = DatabaseConfig.GetDatabaseConnection();
            await openDatabaseConnection.OpenAsync();

            using SqlCommand closeSavingsAccountCommand = new SqlCommand(closeSavingsAccountQuery, openDatabaseConnection);
            closeSavingsAccountCommand.Parameters.AddWithValue("@SavingsAccountId", savingsAccountId);

            int numberOfRowsAffected = await closeSavingsAccountCommand.ExecuteNonQueryAsync();
            return numberOfRowsAffected > 0;
        }
    }
}
