
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task<List<Loan>> GetAllLoansAsync()
    {
        return await _loanRepository.GetAllLoansAsync();
    }

    public async Task<Loan> GetLoanByIdAsync(int id)
    {
        if (id <= 0)
            return new Loan();
        return await _loanRepository.GetLoanByIdAsync(id);
    }

    public async Task<List<Loan>> GetLoansByUserAsync(int userId)
    {
        if (userId <= 0)
            return new List<Loan>();
        return await _loanRepository.GetLoansByUserAsync(userId);
    }

    public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
    {
        return await _loanRepository.GetLoansByStatusAsync(loanStatus);

    }

    public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
    {
        return await _loanRepository.GetLoansByTypeAsync(loanType);
    }

    private async Task<(LoanApplicationStatus approved, string? reason)> EvaluateApplicationAsync(LoanApplication application)
    {
        var currentLoans = await _loanRepository.GetLoansByUserAsync(application.UserId);

        decimal totalOutstanding = currentLoans.Sum(l => l.OutstandingBalance);
        int activeLoansCount = currentLoans.Count(l => l.LoanStatus == LoanStatus.Active);

        if (activeLoansCount >= 5)
            return (LoanApplicationStatus.Rejected, "Maximum number of active loans reached.");

        if (totalOutstanding + application.DesiredAmount >= 200000)
            return (LoanApplicationStatus.Rejected, "Total debt limit exceeded.");

        return (LoanApplicationStatus.Approved, null);
    }

    public async Task<LoanApplication> ApplyForLoanAsync(LoanApplicationRequest request)
    {

        _validator.Validate(request);
        var application = new LoanApplication
        {
            UserId = request.UserId,
            LoanType = request.LoanType,
            DesiredAmount = request.DesiredAmount,
            PreferredTermMonths = request.PreferredTermMonths,
            Purpose = request.Purpose,
            ApplicationStatus = LoanApplicationStatus.Pending,
            RejectionReason = ""

        };
        int appId = await _loanRepository.CreateLoanApplicationAsync(request);
        application.Id = appId;
        return application;
    }

    public async Task<(LoanApplicationStatus approved, string? reason)> ProcessApplicationStatusAsync(LoanApplication application)
    {
        var (status, reason) = await EvaluateApplicationAsync(application);

        await _loanRepository.UpdateLoanApplicationStatusAsync(application.Id, status, reason);

        return (status, reason);

    }

    private decimal GetInterestRateForType(LoanType loanType)
    {
        return loanType switch
        {
            LoanType.Personal => 8.5m,
            LoanType.Mortgage => 4.5m,
            LoanType.Student => 3.0m,
            LoanType.Auto => 6.5m
        };
    }

    public async Task<int> AddLoanAsync(LoanApplication application)
    {

        decimal rate = GetInterestRateForType(application.LoanType);
        LoanEstimate estimate = _calculator.computeEstimate(application.DesiredAmount, rate, application.PreferredTermMonths);

        Loan loan = new Loan
        {
            UserId = application.UserId,
            LoanType = application.LoanType,
            Principal = application.DesiredAmount,
            OutstandingBalance = application.DesiredAmount,
            InterestRate = rate,
            MonthlyInstallment = estimate.MonthlyInstallment,
            RemainingMonths = application.PreferredTermMonths,
            LoanStatus = LoanStatus.Active,
            TermInMonths = application.PreferredTermMonths,
            StartDate = DateTime.Now


        };
        return await _loanRepository.CreateLoanAsync(loan);
    }

    public LoanEstimate GetLoanEstimate(LoanApplicationRequest request)
    {
        _validator.Validate(request);

        decimal rate = GetInterestRateForType(request.LoanType);

        return _calculator.computeEstimate(
            request.DesiredAmount,
            rate,
            request.PreferredTermMonths
        );
    }

    public async Task PayInstallmentAsync(int loanId, decimal? customAmount)
    {
        var loan = await _loanRepository.GetLoanByIdAsync(loanId);

        if (loan == null)
            throw new Exception("Loan not found");

        if (loan.RemainingMonths <= 0)
            throw new Exception("Loan already paid");

        if (loan.LoanStatus == LoanStatus.Passed)
            throw new Exception("Loan closed");

        decimal payment = customAmount ?? loan.MonthlyInstallment;
        decimal newBalance;

        if (payment > loan.OutstandingBalance)
        {
            newBalance = 0;
        }

        else newBalance = loan.OutstandingBalance - payment;

        int monthsPaid = customAmount.HasValue
             ? (int)Math.Floor(customAmount.Value / loan.MonthlyInstallment) : 1;

        int newRemainingMonths = Math.Max(0, loan.RemainingMonths - monthsPaid);

        LoanStatus newStatus = loan.LoanStatus;

        if (newBalance <= 0 || newRemainingMonths == 0)
            newStatus = LoanStatus.Passed;

        await _loanRepository.UpdateLoanAfterPaymentAsync(loan.Id, newBalance, newRemainingMonths, newStatus);
    }

    public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
    {
        var rows = await _loanRepository.GetAmortizationAsync(loanId);

        if (rows == null || rows.Count == 0)
        {
            await GenerateAmortizationAsync(loanId);
            rows = await _loanRepository.GetAmortizationAsync(loanId);

        }

        bool isCurrentSet = false;
        foreach (var row in rows)
        {
            if (isCurrentSet && row.DueDate.Date >= DateTime.Today)
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

    public async Task SaveAmortizationAsync(List<AmortizationRow> rows)
    {
        await _loanRepository.SaveAmortizationAsync(rows);
    }

    public async Task GenerateAmortizationAsync(int loanId)
    {

        var loan = await _loanRepository.GetLoanByIdAsync(loanId);
        var rows = _calculator.generate(loan);
        await _loanRepository.SaveAmortizationAsync(rows);

    }
}