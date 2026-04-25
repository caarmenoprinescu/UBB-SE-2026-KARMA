// <copyright file="LoanRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL-backed repository for loans and loan applications.
/// </summary>
public class LoanRepository : ILoanRepository
{
    private const int CommandTimeoutSeconds = 120;

    /// <summary>
    /// Retrieves all loans from storage.
    /// </summary>
    /// <returns>The complete list of loans.</returns>
    public async Task<List<Loan>> GetAllLoansAsync()
    {
        List<Loan> loans = [];

        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = "SELECT * FROM Loan";

        using var command = new SqlCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(this.ReaderToLoan(reader));
        }

        return loans;
    }

    /// <summary>
    /// Retrieves a loan by its identifier.
    /// </summary>
    /// <param name="id">The loan identifier.</param>
    /// <returns>The matching loan, or <see langword="null"/> when not found.</returns>
    public async Task<Loan> GetLoanByIdAsync(int id)
    {
        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = "SELECT * FROM Loan WHERE id = @id";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        command.Parameters.Add("@id", SqlDbType.Int).Value = id;

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return this.ReaderToLoan(reader);
        }

        return null;
    }

    /// <summary>
    /// Retrieves all loans belonging to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The loans owned by the user.</returns>
    public async Task<List<Loan>> GetLoansByUserAsync(int userId)
    {
        List<Loan> loans = [];

        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = "SELECT * FROM Loan WHERE userId = @userId";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(this.ReaderToLoan(reader));
        }

        return loans;
    }

    /// <summary>
    /// Retrieves loans filtered by type.
    /// </summary>
    /// <param name="loanType">The loan type to filter by.</param>
    /// <returns>The loans matching the requested type.</returns>
    public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
    {
        List<Loan> loans = [];

        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = "SELECT * FROM Loan WHERE loanType = @loanType";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@loanType", SqlDbType.NVarChar, 50).Value = loanType.ToString();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(this.ReaderToLoan(reader));
        }

        return loans;
    }

    /// <summary>
    /// Retrieves loans filtered by status.
    /// </summary>
    /// <param name="loanStatus">The loan status to filter by.</param>
    /// <returns>The loans matching the requested status.</returns>
    public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
    {
        List<Loan> loans = [];

        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = "SELECT * FROM Loan WHERE loanStatus = @loanStatus";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        command.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = loanStatus.ToString();

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(this.ReaderToLoan(reader));
        }

        return loans;
    }

    /// <summary>
    /// Saves an amortization schedule for a loan.
    /// </summary>
    /// <param name="rows">The amortization rows to persist.</param>
    /// <returns>A task that completes when persistence finishes.</returns>
    public async Task SaveAmortizationAsync(List<AmortizationRow> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return;
        }

        using var connection = DatabaseConfig.GetDatabaseConnection();
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        try
        {
            var loanId = rows[0].LoanId;

            var deleteQuery = "DELETE FROM AmortizationRow WHERE loanId = @LoanId";
            using (var deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
            {
                deleteCmd.Parameters.Add("@LoanId", SqlDbType.Int).Value = loanId;
                await deleteCmd.ExecuteNonQueryAsync();
            }

            var insertQuery = @"INSERT INTO AmortizationRow 
                        (loanId, installmentNumber, dueDate, principalPortion, interestPortion, remainingBalance) 
                        VALUES 
                        (@LoanId, @InstallmentNumber, @DueDate, @PrincipalPortion, @InterestPortion, @RemainingBalance)";

            using (var insertCmd = new SqlCommand(insertQuery, connection, transaction))
            {
                insertCmd.Parameters.Add("@LoanId", SqlDbType.Int);
                insertCmd.Parameters.Add("@InstallmentNumber", SqlDbType.Int);
                insertCmd.Parameters.Add("@DueDate", SqlDbType.DateTime);
                insertCmd.Parameters.Add("@PrincipalPortion", SqlDbType.Decimal);
                insertCmd.Parameters.Add("@InterestPortion", SqlDbType.Decimal);
                insertCmd.Parameters.Add("@RemainingBalance", SqlDbType.Decimal);

                foreach (var row in rows)
                {
                    insertCmd.Parameters["@LoanId"].Value = row.LoanId;
                    insertCmd.Parameters["@InstallmentNumber"].Value = row.InstallmentNumber;
                    insertCmd.Parameters["@DueDate"].Value = row.DueDate;
                    insertCmd.Parameters["@PrincipalPortion"].Value = row.PrincipalPortion;
                    insertCmd.Parameters["@InterestPortion"].Value = row.InterestPortion;
                    insertCmd.Parameters["@RemainingBalance"].Value = row.RemainingBalance;

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Retrieves amortization rows for a loan.
    /// </summary>
    /// <param name="loanId">The loan identifier.</param>
    /// <returns>The amortization schedule rows.</returns>
    public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
    {
        List<AmortizationRow> rows = [];

        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = @"SELECT id, loanId, installmentNumber, dueDate, 
                             principalPortion, interestPortion, remainingBalance 
                             FROM AmortizationRow 
                             WHERE loanId = @LoanId 
                             ORDER BY installmentNumber ASC";

        using (var command = new SqlCommand(query, connection))
        {
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.AddWithValue("@LoanId", loanId);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var row = new AmortizationRow
                    {
                        Id = (int)reader["id"],
                        LoanId = (int)reader["loanId"],
                        InstallmentNumber = (int)reader["installmentNumber"],
                        DueDate = (DateTime)reader["dueDate"],
                        PrincipalPortion = (decimal)reader["principalPortion"],
                        InterestPortion = (decimal)reader["interestPortion"],
                        RemainingBalance = (decimal)reader["remainingBalance"],
                    };

                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    /// <summary>
    /// Creates a new loan application.
    /// </summary>
    /// <param name="application">The application payload to persist.</param>
    /// <returns>The created loan application identifier.</returns>
    public async Task<int> CreateLoanApplicationAsync(LoanApplicationRequest application)
    {
        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = @"INSERT INTO LoanApplication
            (loanType, desiredAmount, preferredTermMonths, purpose, applicationStatus, rejectionReason, userId)
            OUTPUT INSERTED.id
            VALUES
            (@loanType, @amount, @term, @purpose, @status, @reason, @userId)";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        command.Parameters.AddWithValue("@loanType", application.LoanType.ToString());
        command.Parameters.AddWithValue("@amount", application.DesiredAmount);
        command.Parameters.AddWithValue("@term", application.PreferredTermMonths);
        command.Parameters.AddWithValue("@purpose", application.Purpose);
        command.Parameters.AddWithValue("@status", LoanApplicationStatus.Pending.ToString());
        command.Parameters.AddWithValue("@reason", DBNull.Value);
        command.Parameters.AddWithValue("@userId", application.UserId);

        var newIdentificationNumber = (int)(await command.ExecuteScalarAsync())!;
        return newIdentificationNumber;
    }

    /// <summary>
    /// Updates review status and optional rejection reason for an application.
    /// </summary>
    /// <param name="id">The loan application identifier.</param>
    /// <param name="loanApplicationStatus">The new application status.</param>
    /// <param name="reason">The optional rejection reason.</param>
    /// <returns>A task that completes when the update is applied.</returns>
    public async Task UpdateLoanApplicationStatusAsync(
        int id,
        LoanApplicationStatus loanApplicationStatus,
        string? reason)
    {
        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = @"UPDATE LoanApplication
                             SET rejectionReason = @rejectionReason,
                                 applicationStatus = @loanApplicationStatus
                             WHERE id = @id";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@loanApplicationStatus", SqlDbType.NVarChar, 50).Value =
            loanApplicationStatus.ToString();
        command.Parameters.Add("@rejectionReason", SqlDbType.NVarChar, 255).Value =
            reason != null ? reason : DBNull.Value;

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates a new loan record.
    /// </summary>
    /// <param name="loan">The loan to persist.</param>
    /// <returns>The created loan identifier.</returns>
    public async Task<int> CreateLoanAsync(Loan loan)
    {
        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = @"INSERT INTO Loan
            (userId, loanType, principal, outstandingBalance, interestRate, monthlyInstallment, remainingMonths, loanStatus, termInMonths ,startDate)
            OUTPUT INSERTED.id
            VALUES
            (@userId, @loanType, @principal, @outstandingBalance, @interestRate, @monthlyInstallment, @remainingMonths, @loanStatus, @termInMonths , @startDate)";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        command.Parameters.Add("@userId", SqlDbType.Int).Value = loan.UserIdentificationNumber;
        command.Parameters.AddWithValue("@loanType", loan.LoanType.ToString());
        command.Parameters.AddWithValue("@principal", loan.Principal);
        command.Parameters.AddWithValue("@outstandingBalance", loan.OutstandingBalance);
        command.Parameters.AddWithValue("@interestRate", loan.InterestRate);
        command.Parameters.AddWithValue("@monthlyInstallment", loan.MonthlyInstallment);
        command.Parameters.AddWithValue("@remainingMonths", loan.RemainingMonths);
        command.Parameters.AddWithValue("@loanStatus", loan.LoanStatus.ToString());
        command.Parameters.AddWithValue("@termInMonths", loan.TermInMonths);
        command.Parameters.AddWithValue("@startDate", loan.StartDate);

        var newIdentificationNumber = (int)(await command.ExecuteScalarAsync())!;
        return newIdentificationNumber;
    }

    /// <summary>
    /// Updates a loan after a payment is processed.
    /// </summary>
    /// <param name="id">The loan identifier.</param>
    /// <param name="newBalance">The updated outstanding balance.</param>
    /// <param name="newRemainingMonths">The updated remaining term.</param>
    /// <param name="newStatus">The updated loan status.</param>
    /// <returns>A task that completes when the update is applied.</returns>
    public async Task UpdateLoanAfterPaymentAsync(
        int id,
        decimal newBalance,
        int newRemainingMonths,
        LoanStatus newStatus)
    {
        using var connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        var query = @"UPDATE Loan
                             SET outstandingBalance = @outstandingBalance,
                                 remainingMonths = @remainingMonths,
                                 loanStatus = @loanStatus
                             WHERE id = @id";

        using var command = new SqlCommand(query, connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        command.Parameters.Add("@outstandingBalance", SqlDbType.Decimal).Value = newBalance;
        command.Parameters.Add("@remainingMonths", SqlDbType.Int).Value = newRemainingMonths;
        command.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = newStatus.ToString();
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;

        await command.ExecuteNonQueryAsync();
    }

    private Loan ReaderToLoan(SqlDataReader reader)
    {
        return new Loan
        {
            IdentificationNumber = (int)reader["id"],
            UserIdentificationNumber = (int)reader["userId"],
            LoanType = Enum.Parse<LoanType>(reader["loanType"].ToString()!, true),
            Principal = (decimal)reader["principal"],
            OutstandingBalance = (decimal)reader["outstandingBalance"],
            InterestRate = (decimal)reader["interestRate"],
            MonthlyInstallment = (decimal)reader["monthlyInstallment"],
            RemainingMonths = (int)reader["remainingMonths"],
            LoanStatus = Enum.Parse<LoanStatus>(reader["loanStatus"].ToString()!, true),
            TermInMonths = (int)reader["termInMonths"],
            StartDate = (DateTime)reader["startDate"],
        };
    }
}