using System.Collections.Generic;
<<<<<<< HEAD
using System.Data.SqlClient;

public interface ILoanRepository
{
    List<Loan> GetAllLoans();

    List<Loan> GetLoansByUser(int userId);

    List<Loan> GetLoansByStatus(string loanStatus);

    List<Loan> GetLoansByType(string loanType);

}
=======

public interface ILoanRepository
{
    List<Loan> getAllLoans();
    Loan getById(int id);
    List<Loan> getByUser(int userID);
    void SaveAmortization(List<AmortizationRow> rows);
    List<AmortizationRow> GetAmortization(int loanId);
}
>>>>>>> main
