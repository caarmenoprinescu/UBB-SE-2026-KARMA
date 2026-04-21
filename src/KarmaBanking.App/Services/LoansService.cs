// <copyright file="LoansService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LoanService : ILoanService
{
    private readonly AmortizationCalculator _calculator;
    private readonly ILoanRepository _loanRepository;
    private readonly LoanApplicationValidator _validator;

    public LoanService(ILoanRepository loanRepository)
    {
        this._loanRepository = loanRepository;
        this._validator = new LoanApplicationValidator();
        this._calculator = new AmortizationCalculator();
    }

    public async Task<List<Loan>> GetAllLoansAsync()
    {
        return await this._loanRepository.GetAllLoansAsync();
    }

    public async Task<Loan> GetLoanByIdAsync(int id)
    {
        if (id <= 0)
        {
            return new Loan();
        }

        return await this._loanRepository.GetLoanByIdAsync(id);
    }

    public async Task<List<Loan>> GetLoansByUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return [];
        }

        return await this._loanRepository.GetLoansByUserAsync(userId);
    }

    public async Task<List<Loan>> GetLoansByStatusAsync(LoanStatus loanStatus)
    {
        return await this._loanRepository.GetLoansByStatusAsync(loanStatus);
    }

    public async Task<List<Loan>> GetLoansByTypeAsync(LoanType loanType)
    {
        return await this._loanRepository.GetLoansByTypeAsync(loanType);
    }

    public async Task<LoanApplication> ApplyForLoanAsync(LoanApplicationRequest request)
    {
        this._validator.Validate(request);
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
        var appId = await this._loanRepository.CreateLoanApplicationAsync(request);
        application.Id = appId;
        return application;
    }

    public async Task<(LoanApplicationStatus approved, string? reason)> ProcessApplicationStatusAsync(
        LoanApplication application)
    {
        var (status, reason) = await this.EvaluateApplicationAsync(application);

        await this._loanRepository.UpdateLoanApplicationStatusAsync(application.Id, status, reason);

        return (status, reason);
    }

    public async Task<int> AddLoanAsync(LoanApplication application)
    {
        var rate = this.GetInterestRateForType(application.LoanType);
        var estimate = this._calculator.computeEstimate(
            application.DesiredAmount,
            rate,
            application.PreferredTermMonths);

        var loan = new Loan
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
        return await this._loanRepository.CreateLoanAsync(loan);
    }

    public LoanEstimate GetLoanEstimate(LoanApplicationRequest request)
    {
        this._validator.Validate(request);

        var rate = this.GetInterestRateForType(request.LoanType);

        return this._calculator.computeEstimate(
            request.DesiredAmount,
            rate,
            request.PreferredTermMonths);
    }

    public async Task PayInstallmentAsync(int loanId, decimal? customAmount)
    {
        var loan = await this._loanRepository.GetLoanByIdAsync(loanId);

        if (loan == null)
        {
            throw new InvalidOperationException("Loan not found.");
        }

        if (loan.RemainingMonths <= 0)
        {
            throw new InvalidOperationException("This loan is already closed.");
        }

        if (loan.LoanStatus == LoanStatus.Passed)
        {
            throw new InvalidOperationException("This loan is already closed.");
        }

        var payment = customAmount ?? loan.MonthlyInstallment;

        if (payment <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.");
        }

        if (customAmount.HasValue && payment < loan.MonthlyInstallment)
        {
            throw new InvalidOperationException("Payment amount must be at least the minimum installment.");
        }

        if (payment > loan.OutstandingBalance)
        {
            throw new InvalidOperationException("Payment amount exceeds the outstanding balance.");
        }

        decimal newBalance;

        newBalance = loan.OutstandingBalance - payment;

        var monthsPaid = customAmount.HasValue
            ? (int)Math.Floor(customAmount.Value / loan.MonthlyInstallment)
            : 1;

        var newRemainingMonths = Math.Max(0, loan.RemainingMonths - monthsPaid);

        var newStatus = loan.LoanStatus;

        if (newBalance <= 0 || newRemainingMonths == 0)
        {
            newStatus = LoanStatus.Passed;
        }

        await this._loanRepository.UpdateLoanAfterPaymentAsync(loan.Id, newBalance, newRemainingMonths, newStatus);
    }

    public async Task<List<AmortizationRow>> GetAmortizationAsync(int loanId)
    {
        var rows = await this._loanRepository.GetAmortizationAsync(loanId);

        if (rows == null || rows.Count == 0)
        {
            await this.GenerateAmortizationAsync(loanId);
            rows = await this._loanRepository.GetAmortizationAsync(loanId);
        }

        var isCurrentSet = false;
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
        await this._loanRepository.SaveAmortizationAsync(rows);
    }

    public async Task GenerateAmortizationAsync(int loanId)
    {
        var loan = await this._loanRepository.GetLoanByIdAsync(loanId);
        var rows = this._calculator.generate(loan);
        await this._loanRepository.SaveAmortizationAsync(rows);
    }

    private async Task<(LoanApplicationStatus approved, string? reason)> EvaluateApplicationAsync(
        LoanApplication application)
    {
        var currentLoans = await this._loanRepository.GetLoansByUserAsync(application.UserId);

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