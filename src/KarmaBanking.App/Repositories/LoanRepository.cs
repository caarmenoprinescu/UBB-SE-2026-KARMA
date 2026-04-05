using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

public class LoanRepository : ILoanRepository
{
    public List<Loan> GetAllLoans()


    {

        System.Diagnostics.Debug.WriteLine("User: " + System.Security.Principal.WindowsIdentity.GetCurrent().Name);
        System.Diagnostics.Debug.WriteLine("ConnStr: " + DatabaseConfig.ConnectionString);

        List<Loan> loans = new List<Loan>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan";

            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = ReaderToLoan(reader);
                loans.Add(loan);
            }
        }

        return loans;
    }

    public Loan GetById(int id)
    {
        Loan loan = null;

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan WHERE id = @id";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                loan = ReaderToLoan(reader);
            }
        }

        return loan;
    }

    public List<Loan> GetLoansByUser(int userId)
    {
        List<Loan> loans = new List<Loan>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan WHERE userId = @userId";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@userId", SqlDbType.Int).Value = userId;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = ReaderToLoan(reader);
                loans.Add(loan);
            }
        }

        return loans;
    }

    public List<Loan> GetLoansByType(string loanType)
    {
        List<Loan> loans = new List<Loan>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan WHERE loanType = @loanType";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@loanType", SqlDbType.NVarChar, 50).Value = loanType;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = ReaderToLoan(reader);
                loans.Add(loan);
            }
        }

        return loans;
    }

    public List<Loan> GetLoansByStatus(string loanStatus)
    {
        List<Loan> loans = new List<Loan>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan WHERE loanStatus = @loanStatus";

            SqlCommand cmd = new SqlCommand(query, connection);
            cmd.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = loanStatus;

            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = ReaderToLoan(reader);
                loans.Add(loan);
            }
        }

        return loans;
    }

    public void SaveAmortization(List<AmortizationRow> rows)
    {
        if (rows == null || rows.Count == 0) return;

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            using (SqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    int loanId = rows[0].LoanId;

                    string deleteQuery = "DELETE FROM AmortizationRow WHERE loanId = @LoanId";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, connection, transaction))
                    {
                        deleteCmd.Parameters.AddWithValue("@LoanId", loanId);
                        deleteCmd.ExecuteNonQuery();
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

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public List<AmortizationRow> GetAmortization(int loanId)
    {
        List<AmortizationRow> rows = new List<AmortizationRow>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = @"SELECT id, loanId, installmentNumber, dueDate, 
                             principalPortion, interestPortion, remainingBalance 
                             FROM AmortizationRow 
                             WHERE loanId = @LoanId 
                             ORDER BY installmentNumber ASC";

            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@LoanId", loanId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
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
        }

        bool isCurrentSet = false;

        foreach (var row in rows)
        {
            if (!isCurrentSet && row.DueDate.Date >= DateTime.Today)
            {
                row.IsCurrent = true;
                isCurrentSet = true;
            }
            else
            {
                row.IsCurrent = false;
            }
        }

        return rows;
    }

    private Loan ReaderToLoan(SqlDataReader reader)
{
    return new Loan
    {
        id = (int)reader["id"],
        userId = (int)reader["userId"],
        loanType = reader["loanType"].ToString(),
        principal = (decimal)reader["principal"],
        outstandingBalance = (decimal)reader["outstandingBalance"],
        interestRate = (decimal)reader["interestRate"],
        monthlyInstallment = (decimal)reader["monthlyInstallment"],
        remainingMonths = (int)reader["remainingMonths"],
        loanStatus = reader["loanStatus"].ToString(),
        TermInMonths = reader["TermInMonths"] != DBNull.Value ? (int)reader["TermInMonths"] : 0,
        StartDate = reader["StartDate"] != DBNull.Value ? (DateTime)reader["StartDate"] : default(DateTime)
    };
}
    public void CreateLoanApplication(LoanApplication app)
{
    using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
    {
        connection.Open();

        string query = @"INSERT INTO LoanApplication
            (loanType, desiredAmount, preferredTermMonths, purpose, applicationStatus, rejectionReason)
            VALUES
            (@loanType, @amount, @term, @purpose, @status, @reason)";

        using (SqlCommand cmd = new SqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@loanType", app.loanType.ToString());
            cmd.Parameters.AddWithValue("@amount", app.desiredAmount);
            cmd.Parameters.AddWithValue("@term", app.preferredTermMonths);
            cmd.Parameters.AddWithValue("@purpose", app.purpose);
            cmd.Parameters.AddWithValue("@status", app.applicationStatus.ToString());
            cmd.Parameters.AddWithValue("@reason", (object?)app.rejectionReason ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }
    }
}

    public Loan pay(int id, decimal amount)
    {
        Loan loan = GetById(id);

        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found.");
        }

        loan.outstandingBalance -= amount;
        loan.remainingMonths = Math.Max(0, loan.remainingMonths - 1);

        if (loan.outstandingBalance <= 0)
        {
            loan.outstandingBalance = 0;
            loan.loanStatus = LoanStatus.Passed.ToString();
        }

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = @"UPDATE Loan
                             SET outstandingBalance = @outstandingBalance,
                                 remainingMonths = @remainingMonths,
                                 loanStatus = @loanStatus
                             WHERE id = @id";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.Add("@outstandingBalance", SqlDbType.Decimal).Value = loan.outstandingBalance;
                command.Parameters.Add("@remainingMonths", SqlDbType.Int).Value = loan.remainingMonths;
                command.Parameters.Add("@loanStatus", SqlDbType.NVarChar, 50).Value = loan.loanStatus;
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;

                command.ExecuteNonQuery();
            }
        }

        return loan;
    }

    public void MakePayment(int loanId, decimal amount)
    {
        pay(loanId, amount);
    }

}
