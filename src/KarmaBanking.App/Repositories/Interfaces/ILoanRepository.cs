using System.Collections.Generic;

public interface ILoanRepository
{
    List<Loan> getAllLoans();
    Loan getById(int id);
    List<Loan> getByUser(int userID);
    void SaveAmortization(List<AmortizationRow> rows);
    List<AmortizationRow> GetAmortization(int loanId);
}
