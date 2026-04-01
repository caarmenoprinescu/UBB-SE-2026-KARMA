using System.Collections.Generic;

public interface ILoanRepository
{
    List<Loan> GetAllLoans();

    Loan GetById(int id);

    List<Loan> GetLoansByUser(int userId);

    List<Loan> GetLoansByStatus(string loanStatus);

    List<Loan> GetLoansByType(string loanType);

    void SaveAmortization(List<AmortizationRow> rows);

    List<AmortizationRow> GetAmortization(int loanId);

    void CreateLoanApplication(LoanApplication request);

    Loan pay(int id, decimal amount);

    void MakePayment(int loanId, decimal amount);
}
