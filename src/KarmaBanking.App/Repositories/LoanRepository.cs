using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class LoanRepository : ILoanRepository
{

    public async Task<List<Loan>> GetAllLoansAsync()
    {

        List<Loan> loans = new List<Loan>();

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = "SELECT * FROM Loan";

        using SqlCommand command = new SqlCommand(query, connection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {

            loans.Add(ReaderToLoan(reader));
        }

        return loans;
    }

    private Loan ReaderToLoan(SqlDataReader reader)
    {
        return new Loan
        {
            Id = (int)reader["id"],
            UserId = (int)reader["userId"],
            LoanType = Enum.Parse<LoanType>(reader["loanType"].ToString(), ignoreCase:true),
            Principal = (decimal)reader["principal"],
            OutstandingBalance = (decimal)reader["outstandingBalance"],
            InterestRate = (decimal)reader["interestRate"],
            MonthlyInstallment = (decimal)reader["monthlyInstallment"],
            RemainingMonths = (int)reader["remainingMonths"],
            LoanStatus = Enum.Parse<LoanStatus>(reader["loanStatus"].ToString(), ignoreCase:true),
            TermInMonths = (int)reader["termInMonths"],
            StartDate = (DateTime)reader["startDate"]
        };
    }

    public async Task<Loan> GetLoanByIdAsync(int id)
    {
        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = "SELECT * FROM Loan WHERE id = @id";

        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.Add("@id", SqlDbType.Int).Value = id;

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return ReaderToLoan(reader);
        }

        return null;
    }

    public async Task<List<Loan>> GetLoansByUserAsync(int userId)
    {
        List<Loan> loans = new List<Loan>();

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = "SELECT * FROM Loan WHERE userId = @userId";

        using SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
    {
        List<Loan> loans = new List<Loan>();

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = "SELECT * FROM Loan WHERE loanType = @loanType";

        using SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.Add("@loanType", SqlDbType.NVarChar, 50).Value = loanType.ToString();

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReaderToLoan(reader));
        }

        return loans;
    }

    public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
    {
        List<Loan> loans = new List<Loan>();

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = "SELECT * FROM Loan WHERE loanStatus = @loanStatus";

        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = loanStatus.ToString();

        using SqlDataReader reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            loans.Add(ReaderToLoan(reader));
        }

        return loans;
    }

    public async Task SaveAmortizationAsync(List<AmortizationRow> rows)
    {
        if (rows == null || rows.Count == 0) return;

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();
        await connection.OpenAsync();

        using SqlTransaction transaction = connection.BeginTransaction();

        try
        {
            int loanId = rows[0].LoanId;

            string deleteQuery = "DELETE FROM AmortizationRow WHERE loanId = @LoanId";
            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
            {
                deleteCmd.Parameters.Add("@LoanId", SqlDbType.Int).Value = loanId;
                await deleteCmd.ExecuteNonQueryAsync();
            }

            string insertQuery = @"INSERT INTO AmortizationRow 
                        (loanId, installmentNumber, dueDate, principalPortion, interestPortion, remainingBalance) 
                        VALUES 
                        (@LoanId, @InstallmentNumber, @DueDate, @PrincipalPortion, @InterestPortion, @RemainingBalance)";

            using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection, transaction))
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

    public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
    {
        List<AmortizationRow> rows = new List<AmortizationRow>();

        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = @"SELECT id, loanId, installmentNumber, dueDate, 
                             principalPortion, interestPortion, remainingBalance 
                             FROM AmortizationRow 
                             WHERE loanId = @LoanId 
                             ORDER BY installmentNumber ASC";

        using (SqlCommand command = new SqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@LoanId", loanId);

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
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
                        RemainingBalance = (decimal)reader["remainingBalance"]
                    };

                    rows.Add(row);
                }

            }
        }

        return rows;
    }

    public async Task<int> CreateLoanApplicationAsync(LoanApplicationRequest application)
    {
        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = @"INSERT INTO LoanApplication
            (loanType, desiredAmount, preferredTermMonths, purpose, applicationStatus, rejectionReason, userId)
            OUTPUT INSERTED.id
            VALUES
            (@loanType, @amount, @term, @purpose, @status, @reason, @userId)";

        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.AddWithValue("@loanType", application.LoanType.ToString());
        command.Parameters.AddWithValue("@amount", application.DesiredAmount);
        command.Parameters.AddWithValue("@term", application.PreferredTermMonths);
        command.Parameters.AddWithValue("@purpose", application.Purpose);
        command.Parameters.AddWithValue("@status", LoanApplicationStatus.Pending.ToString());
        command.Parameters.AddWithValue("@reason", DBNull.Value);
        command.Parameters.AddWithValue("@userId", application.UserId);

        int newId = (int)await command.ExecuteScalarAsync();
        return newId;


    }


   public async Task UpdateLoanApplicationStatusAsync(int id, LoanApplicationStatus loanApplicationStatus, string? reason)
    {


        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = @"UPDATE LoanApplication
                             SET rejectionReason = @rejectionReason,
                                 applicationStatus = @loanApplicationStatus
                             WHERE id = @id";

        using SqlCommand command = new SqlCommand(query, connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@loanApplicationStatus", SqlDbType.NVarChar, 50).Value = loanApplicationStatus.ToString();
        command.Parameters.Add("@rejectionReason", SqlDbType.NVarChar, 255).Value = reason != null ? reason.ToString() : DBNull.Value;

        await command.ExecuteNonQueryAsync();

    }

    public async Task<int> CreateLoanAsync(Loan loan)
    {
        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = @"INSERT INTO Loan
            (userId, loanType, principal, outstandingBalance, interestRate, monthlyInstallment, remainingMonths, loanStatus, termInMonths ,startDate)
            OUTPUT INSERTED.id
            VALUES
            (@userId, @loanType, @principal, @outstandingBalance, @interestRate, @monthlyInstallment, @remainingMonths, @loanStatus, @termInMonths , @startDate)";

        using SqlCommand command = new SqlCommand(query, connection);

        command.Parameters.Add("@userId", SqlDbType.Int).Value = loan.UserId;
        command.Parameters.AddWithValue("@loanType", loan.LoanType.ToString());
        command.Parameters.AddWithValue("@principal", loan.Principal);
        command.Parameters.AddWithValue("@outstandingBalance", loan.OutstandingBalance);
        command.Parameters.AddWithValue("@interestRate", loan.InterestRate);
        command.Parameters.AddWithValue("@monthlyInstallment", loan.MonthlyInstallment);
        command.Parameters.AddWithValue("@remainingMonths", loan.RemainingMonths);
        command.Parameters.AddWithValue("@loanStatus", loan.LoanStatus.ToString());
        command.Parameters.AddWithValue("@termInMonths", loan.TermInMonths);
        command.Parameters.AddWithValue("@startDate", loan.StartDate);

        int newId = (int)await command.ExecuteScalarAsync();
        return newId;
    }

    public async Task UpdateLoanAfterPaymentAsync(int id, decimal newBalance, int newRemainingMonths, LoanStatus newStatus)
    {


        using SqlConnection connection = DatabaseConfig.GetDatabaseConnection();

        await connection.OpenAsync();

        string query = @"UPDATE Loan
                             SET outstandingBalance = @outstandingBalance,
                                 remainingMonths = @remainingMonths,
                                 loanStatus = @loanStatus
                             WHERE id = @id";

        using SqlCommand command = new SqlCommand(query, connection);


        command.Parameters.Add("@outstandingBalance", SqlDbType.Decimal).Value = newBalance;
        command.Parameters.Add("@remainingMonths", SqlDbType.Int).Value = newRemainingMonths;
        command.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = newStatus.ToString();
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;

        await command.ExecuteNonQueryAsync();


    }


}
