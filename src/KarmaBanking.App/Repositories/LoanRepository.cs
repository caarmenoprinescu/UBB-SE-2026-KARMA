
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;

public class LoanRepository
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
                    loanStatus = reader["loanStatus"].ToString()
                };
                loans.Add(loan);
            }

        }
        return loans;
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
                    loanStatus = reader["loanStatus"].ToString()
                };

                loans.Add(loan);
            }
            return loans;
        }



    }
}