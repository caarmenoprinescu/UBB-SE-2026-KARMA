using System;
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
    }
}
