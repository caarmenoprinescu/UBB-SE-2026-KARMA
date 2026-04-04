using System;
using System.Collections.Generic;
using Windows.System;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly LoanApplicationValidator _validator;
    private readonly AmortizationCalculator _calculator;

    public LoanService(ILoanRepository loanRepository)
    {
        _loanRepository = loanRepository;
        _validator = new LoanApplicationValidator();
        _calculator = new AmortizationCalculator();
    }

    public List<Loan> GetAllLoans()
    {
        return _loanRepository.GetAllLoans();
    }

    public Loan GetLoanById(int id)
    {
        if (id <= 0)
            return new Loan();
        return _loanRepository.GetById(id);
    }

    public List<Loan> GetLoansByUser(int userId)
    {
        if (userId <= 0)
            return new List<Loan>();
        return _loanRepository.GetLoansByUser(userId);
    }

    public List<Loan> GetLoansByStatus(LoanStatus loanStatus) {
        string statusString = loanStatus.ToString();
        return _loanRepository.GetLoansByStatus(statusString);
    
    }

    public List<Loan> GetLoansByType(LoanType loanType) {
        string typeString = loanType.ToString();
        return _loanRepository.GetLoansByType(typeString);
    }

    public double CalculateRepaymentProgress(Loan loan)
    {
        if (loan == null || loan.Principal == 0)
            return 0;

        double paid = (double)(loan.Principal - loan.OutstandingBalance);
        double progress = (paid / (double)loan.Principal) * 100;

        if (progress < 0) return 0;
        if (progress > 100) return 100;

        return Math.Round(progress, 2);
    }

    public void ApplyForLoan(LoanApplicationRequest request)
    {

        _validator.Validate(request);
        var application = new LoanApplication
        {
            LoanType = request.LoanType,
            DesiredAmount = request.DesiredAmount,
            PreferredTermMonths = request.PreferredTermMonths,
            Purpose = request.Purpose,
            ApplicationStatus = LoanApplicationStatus.Pending,
            RejectionReason = null
        };

        _loanRepository.CreateLoanApplication(application);
    }

    public LoanEstimate GetLoanEstimate(LoanApplicationRequest request)
    {
        _validator.Validate(request);

        decimal rate = request.LoanType switch
        {
            LoanType.Personal => 10,
            LoanType.Auto => 7,
            LoanType.Mortgage => 5,
            LoanType.Student => 3,
            _ => 8
        };

        return _calculator.computeEstimate(
            request.DesiredAmount,
            rate,
            request.PreferredTermMonths
        );
    }

    public void PayInstallment(int loanId)
    {
        var loan = _loanRepository.GetById(loanId);

        if (loan == null)
            throw new Exception("Loan not found");

        if (loan.RemainingMonths <= 0)
            throw new Exception("Loan already paid");

        _loanRepository.MakePayment(loanId, loan.MonthlyInstallment);
    }
}