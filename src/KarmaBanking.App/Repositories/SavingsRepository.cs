using System;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace KarmaBanking.App.Repositories
{
    public class SavingsRepository : ISavingsRepository
    {
        private readonly DatabaseConnection _databaseConnection;

        public SavingsRepository(DatabaseConnection databaseConnection)
        {
            _databaseConnection = databaseConnection;
        }

        public async Task<bool> AddSavingsAccountAsync(SavingsAccount newSavingsAccount)
        {
            const string insertSavingsAccountQuery = @"
                INSERT INTO SavingsAccount 
                    (UserId, FundingAccountId, AccountName, SavingsType, Balance, 
                     AccruedInterest, InterestRate, TargetAmount, TargetDate, 
                     MaturityDate, DepositFrequency, AutoDepositAmount, Status, CreatedAt)
                VALUES 
                    (@UserId, @FundingAccountId, @AccountName, @SavingsType, @Balance,
                     @AccruedInterest, @InterestRate, @TargetAmount, @TargetDate,
                     @MaturityDate, @DepositFrequency, @AutoDepositAmount, @Status, @CreatedAt)";

            using SqlConnection openDatabaseConnection = _databaseConnection.GetDatabaseConnection();
            await openDatabaseConnection.OpenAsync();

            using SqlCommand insertSavingsAccountCommand = new SqlCommand(insertSavingsAccountQuery, openDatabaseConnection);
            insertSavingsAccountCommand.Parameters.AddWithValue("@UserId", newSavingsAccount.UserId);
            insertSavingsAccountCommand.Parameters.AddWithValue("@FundingAccountId", newSavingsAccount.FundingAccountId);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AccountName", newSavingsAccount.AccountName);
            insertSavingsAccountCommand.Parameters.AddWithValue("@SavingsType", newSavingsAccount.SavingsType);
            insertSavingsAccountCommand.Parameters.AddWithValue("@Balance", newSavingsAccount.Balance);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AccruedInterest", newSavingsAccount.AccruedInterest);
            insertSavingsAccountCommand.Parameters.AddWithValue("@InterestRate", newSavingsAccount.InterestRate);
            insertSavingsAccountCommand.Parameters.AddWithValue("@TargetAmount", (object?)newSavingsAccount.TargetAmount ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@TargetDate", (object?)newSavingsAccount.TargetDate ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@MaturityDate", (object?)newSavingsAccount.MaturityDate ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@DepositFrequency", (object?)newSavingsAccount.DepositFrequency ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@AutoDepositAmount", (object?)newSavingsAccount.AutoDepositAmount ?? DBNull.Value);
            insertSavingsAccountCommand.Parameters.AddWithValue("@Status", newSavingsAccount.Status);
            insertSavingsAccountCommand.Parameters.AddWithValue("@CreatedAt", newSavingsAccount.CreatedAt);

            int numberOfRowsAffectedByInsert = await insertSavingsAccountCommand.ExecuteNonQueryAsync();
            return numberOfRowsAffectedByInsert > 0;
        }
    }
}
