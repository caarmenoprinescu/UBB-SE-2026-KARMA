using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILoanRepository
{
    Task<List<Loan>> GetAllLoansAsync();

    Task<Loan> GetLoanByIdAsync(int id);

    Task<List<Loan>> GetLoansByUserAsync(int userId);

    Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus);

    Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType);

    Task SaveAmortizationAsync(List<AmortizationRow> rows);

    Task<List<AmortizationRow>> GetAmortizationAsync(int loanId);

    Task<int> CreateLoanApplicationAsync(LoanApplicationRequest request);

    Task UpdateLoanApplicationStatusAsync(int id, LoanApplicationStatus loanApplicationStatus, string? reason);

    Task<int> CreateLoanAsync(Loan loan);

    Task UpdateLoanAfterPaymentAsync(int id, decimal newBalance, int newRemainingMonths, LoanStatus newStatus);
}
