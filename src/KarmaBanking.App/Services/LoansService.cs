// <copyright file="LoansService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KarmaBanking.App.Services;
using KarmaBanking.App.Utils;

public class LoanService : ILoanService
{
    private readonly ILoanRepository loanRepository;
    private readonly LoanApplicationValidator validator;
    private readonly PaymentCalculationService paymentCalculationService;

    public LoanService(ILoanRepository loanRepository)
    {
        this.loanRepository = loanRepository;
        this.validator = new LoanApplicationValidator();
        this.paymentCalculationService = new PaymentCalculationService();
    }

    public async Task<List<Loan>> GetAllLoansAsync()
    {
        return await this.loanRepository.GetAllLoansAsync();
    }

    public async Task<Loan> GetLoanByIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Loan();
        }

        return await this.loanRepository.GetLoanByIdAsync(id);
    }

    public async Task<List<Loan>> GetLoansByUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return [];
        }

        return await this.loanRepository.GetLoansByUserAsync(userId);
    }

    public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
    {
        return await this.loanRepository.GetLoansByStatusAsync(loanStatus);
    }

    public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
    {
        return await this.loanRepository.GetLoansByTypeAsync(loanType);
    }

    public async Task<LoanApplication> ApplyForLoanAsync(LoanApplicationRequest request)
    {
        this.validator.Validate(request);

        var application = new LoanApplication
        {
            UserIdentificationNumber = request.UserId,
            LoanType = request.LoanType,
            DesiredAmount = request.DesiredAmount,
            PreferredTermMonths = request.PreferredTermMonths,
            Purpose = request.Purpose,
            ApplicationStatus = LoanApplicationStatus.Pending,
            RejectionReason = string.Empty,
        };

        var appId = await this.loanRepository.CreateLoanApplicationAsync(request);
        application.IdentificationNumber = appId;

        return application;
    }

    public async Task<(LoanApplicationStatus Status, string? RejectionReason)> SubmitLoanApplicationAsync(LoanApplicationRequest request)
    {
        var newApplication = await this.ApplyForLoanAsync(request);
        var (status, rejectionReason) = await this.ProcessApplicationStatusAsync(newApplication);

        if (status == LoanApplicationStatus.Approved)
        {
            var loanId = await this.AddLoanAsync(newApplication);
            await this.GenerateAmortizationAsync(loanId);
        }

        return (status, rejectionReason);
    }

    public async Task<(LoanApplicationStatus approved, string? reason)> ProcessApplicationStatusAsync(LoanApplication application)
    {
        var (status, reason) = await this.EvaluateApplicationAsync(application);

        await this.loanRepository.UpdateLoanApplicationStatusAsync(application.IdentificationNumber, status, reason);

        return (status, reason);
    }

    public async Task<int> AddLoanAsync(LoanApplication application)
    {
        var rate = this.GetInterestRateForType(application.LoanType);
        var estimate = AmortizationCalculator.ComputeEstimate(
            application.DesiredAmount,
            rate,
            application.PreferredTermMonths);

        var loan = new Loan
        {
            UserIdentificationNumber = application.UserIdentificationNumber,
            LoanType = application.LoanType,
            Principal = application.DesiredAmount,
            OutstandingBalance = application.DesiredAmount,
            InterestRate = rate,
            MonthlyInstallment = estimate.MonthlyInstallment,
            RemainingMonths = application.PreferredTermMonths,
            LoanStatus = LoanStatus.Active,
            TermInMonths = application.PreferredTermMonths,
            StartDate = DateTime.Now,
        };

        return await this.loanRepository.CreateLoanAsync(loan);
    }

    public LoanEstimate GetLoanEstimate(LoanApplicationRequest request)
    {
        this.validator.Validate(request);

        var rate = this.GetInterestRateForType(request.LoanType);

        return AmortizationCalculator.ComputeEstimate(
            request.DesiredAmount,
            rate,
            request.PreferredTermMonths);
    }

    public async Task PayInstallmentAsync(int loanId, decimal? customAmount)
    {
        var loan = await this.loanRepository.GetLoanByIdAsync(loanId);

        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found.");
        }

        if (loan.RemainingMonths <= 0 || loan.LoanStatus == LoanStatus.Passed)
        {
            throw new InvalidOperationException("This loan is already closed.");
        }

        var paymentAmount = customAmount ?? loan.MonthlyInstallment;

        if (paymentAmount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        if (customAmount.HasValue && paymentAmount < loan.MonthlyInstallment)
        {
            throw new InvalidOperationException("Payment amount must be at least the minimum installment.");
        }

        if (paymentAmount > loan.OutstandingBalance)
        {
            throw new InvalidOperationException("Payment amount exceeds the outstanding balance.");
        }

        var (newBalance, newRemainingMonths) = this.CalculatePaymentPreview(loan, customAmount);

        var newStatus = newBalance <= 0 || newRemainingMonths == 0
            ? LoanStatus.Passed
            : loan.LoanStatus;

        await this.loanRepository.UpdateLoanAfterPaymentAsync(loan.IdentificationNumber, newBalance, newRemainingMonths, newStatus);
    }

    public (decimal BalanceAfterPayment, int RemainingMonths) CalculatePaymentPreview(Loan loan, decimal? customAmount = null)
    {
        var isStandardPayment = !customAmount.HasValue;
        var customPaymentAmount = customAmount ?? 0m;

        return this.paymentCalculationService.CalculatePaymentPreview(
            loan.MonthlyInstallment,
            loan.OutstandingBalance,
            loan.RemainingMonths,
            isStandardPayment,
            customPaymentAmount);
    }

    public decimal? ParseCustomPaymentAmount(string input)
    {
        var (success, amount) = this.paymentCalculationService.ParsePaymentAmount(input);
        return success ? amount : null;
    }

    public decimal NormalizeCustomPaymentAmount(Loan loan, decimal? currentCustomAmount)
    {
        return this.paymentCalculationService.GetInitialCustomAmount(
            loan.MonthlyInstallment,
            loan.OutstandingBalance,
            currentCustomAmount.HasValue ? (double?)currentCustomAmount.Value : null);
    }

    public double GetRepaymentProgress(Loan loan)
    {
        return (double)AmortizationCalculator.ComputeRepaymentProgress(loan.Principal, loan.OutstandingBalance);
    }

    public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
    {
        var rows = await this.loanRepository.GetAmortizationAsync(loanId);

        if (rows == null || rows.Count == 0)
        {
            await this.GenerateAmortizationAsync(loanId);
            rows = await this.loanRepository.GetAmortizationAsync(loanId);
        }

        var isCurrentSet = false;
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

    public async Task SaveAmortizationAsync(List<AmortizationRow> rows)
    {
        await this.loanRepository.SaveAmortizationAsync(rows);
    }

    public async Task GenerateAmortizationAsync(int loanId)
    {
        var loan = await this.loanRepository.GetLoanByIdAsync(loanId);
        var rows = AmortizationCalculator.Generate(loan);
        await this.loanRepository.SaveAmortizationAsync(rows);
    }

    private async Task<(LoanApplicationStatus approved, string? reason)> EvaluateApplicationAsync(LoanApplication application)
    {
        var currentLoans = await this.loanRepository.GetLoansByUserAsync(application.UserIdentificationNumber);

        var totalOutstanding = currentLoans.Sum(l => l.OutstandingBalance);
        var activeLoansCount = currentLoans.Count(l => l.LoanStatus == LoanStatus.Active);

        if (activeLoansCount >= 5)
        {
            return (LoanApplicationStatus.Rejected, "Maximum number of active loans reached.");
        }

        if (totalOutstanding + application.DesiredAmount >= 200000)
        {
            return (LoanApplicationStatus.Rejected, "Total debt limit exceeded.");
        }

        return (LoanApplicationStatus.Approved, null);
    }

    private decimal GetInterestRateForType(LoanType loanType)
    {
        return loanType switch
        {
            LoanType.Personal => 8.5m,
            LoanType.Mortgage => 4.5m,
            LoanType.Student => 3.0m,
            LoanType.Auto => 6.5m,
        };
    }
}