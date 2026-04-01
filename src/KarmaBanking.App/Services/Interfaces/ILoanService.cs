using System.Collections.Generic;
public interface ILoanService
{
    List<Loan> GetAllLoans();

    Loan GetLoanById(int id);

    List<Loan> GetLoansByUser(int userId);

    List<Loan> GetLoansByStatus(LoanStatus loanStatus);

    List<Loan> GetLoansByType(LoanType loanType);

    double CalculateRepaymentProgress(Loan loan);

    void ApplyForLoan(LoanApplicationRequest request);

}