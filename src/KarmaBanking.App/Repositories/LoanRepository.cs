using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;

public class LoanRepository : ILoanRepository
{
    public List<Loan> getAllLoans()
    {
        List<Loan> loans = new List<Loan>();
        using (SqlConnection connection = new SqlConnection(DatabaseConfig.connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loans";

            SqlCommand command = new SqlCommand(query, connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = new Loan
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
                loans.Add(loan);
            }

        }
        return loans;
    }

    public Loan getById(int id)
    {
        Loan loan = null;
        using (SqlConnection connection = new SqlConnection(DatabaseConfig.connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loan WHERE id = @id";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                loan = new Loan
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
        }
        return loan;
    }

    public List<Loan> getByUser(int userID)
    {
        List<Loan> loans = new List<Loan>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Loans l WHERE l.userId = userID";

            SqlCommand cmd = new SqlCommand(query, connection);
            SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Loan loan = new Loan
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

                loans.Add(loan);
            }
            return loans;
        }
    }

    public void SaveAmortization(List<AmortizationRow> rows)
    {
        if (rows == null || rows.Count == 0) return;

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.connectionString))
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

                    string insertQuery = @"INSERT INTO AmortizationRow (loanId, installmentNumber, dueDate, principalPortion, interestPortion, remainingBalance) 
                                           VALUES (@LoanId, @InstallmentNumber, @DueDate, @PrincipalPortion, @InterestPortion, @RemainingBalance)";
                    
                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection, transaction))
                    {
                        insertCmd.Parameters.Add("@LoanId", System.Data.SqlDbType.Int);
                        insertCmd.Parameters.Add("@InstallmentNumber", System.Data.SqlDbType.Int);
                        insertCmd.Parameters.Add("@DueDate", System.Data.SqlDbType.DateTime);
                        insertCmd.Parameters.Add("@PrincipalPortion", System.Data.SqlDbType.Decimal);
                        insertCmd.Parameters.Add("@InterestPortion", System.Data.SqlDbType.Decimal);
                        insertCmd.Parameters.Add("@RemainingBalance", System.Data.SqlDbType.Decimal);

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

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.connectionString))
        {
            connection.Open();
            string query = "SELECT id, loanId, installmentNumber, dueDate, principalPortion, interestPortion, remainingBalance FROM AmortizationRow WHERE loanId = @LoanId ORDER BY installmentNumber ASC";
            
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
}