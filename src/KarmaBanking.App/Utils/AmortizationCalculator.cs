// <copyright file="AmortizationCalculator.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using System.Collections.Generic;
using KarmaBanking.App.Models;

/// <summary>
/// Provides utility methods for calculating loan amortization schedules and estimates.
/// </summary>
public static class AmortizationCalculator
{
    /// <summary>
    /// Computes a loan estimate based on the requested amount, annual rate, and term.
    /// </summary>
    /// <param name="amount">The desired loan amount.</param>
    /// <param name="annualRate">The annual interest rate as a percentage.</param>
    /// <param name="termMonths">The term of the loan in months.</param>
    /// <returns>A <see cref="LoanEstimate"/> containing the indicative rate, monthly installment, and total repayable amount.</returns>
    public static LoanEstimate ComputeEstimate(decimal amount, decimal annualRate, int termMonths)
    {
        var monthlyRate = annualRate / 12m / 100m;
        decimal monthlyInstallment;

        if (monthlyRate == 0)
        {
            monthlyInstallment = amount / termMonths;
        }
        else
        {
            monthlyInstallment = amount * monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, termMonths) /
                                 ((decimal)Math.Pow(1 + (double)monthlyRate, termMonths) - 1);
        }

        monthlyInstallment = Math.Round(monthlyInstallment, 2);
        var totalRepayable = Math.Round(monthlyInstallment * termMonths, 2);

        return new LoanEstimate
        {
            IndicativeRate = annualRate,
            MonthlyInstallment = monthlyInstallment,
            TotalRepayable = totalRepayable,
        };
    }

    /// <summary>
    /// Computes the repayment progress percentage based on the principal and outstanding balance.
    /// </summary>
    /// <param name="principal">The original principal amount of the loan.</param>
    /// <param name="outstandingBalance">The current outstanding balance of the loan.</param>
    /// <returns>A percentage representing the repayment progress.</returns>
    public static decimal ComputeRepaymentProgress(decimal principal, decimal outstandingBalance)
    {
        if (principal == 0)
        {
            return 0;
        }

        return (principal - outstandingBalance) / principal * 100;
    }

    /// <summary>
    /// Generates an amortization schedule for a given loan.
    /// </summary>
    /// <param name="loan">The loan details used to generate the schedule.</param>
    /// <returns>A list of <see cref="AmortizationRow"/> representing the amortization schedule.</returns>
    public static List<AmortizationRow> Generate(Loan loan)
    {
        var rows = new List<AmortizationRow>();

        var principal = loan.Principal;
        var annualRate = loan.InterestRate;
        var termInMonths = loan.TermInMonths;
        var startDate = loan.StartDate;

        var monthlyRate = annualRate / 12m / 100m;
        var remainingBalance = principal;
        decimal monthlyInstallment;

        if (monthlyRate == 0)
        {
            monthlyInstallment = remainingBalance / termInMonths;
        }
        else
        {
            monthlyInstallment = remainingBalance * monthlyRate *
                                 (decimal)Math.Pow(1 + (double)monthlyRate, termInMonths) /
                                 ((decimal)Math.Pow(1 + (double)monthlyRate, termInMonths) - 1);
        }

        monthlyInstallment = Math.Round(monthlyInstallment, 2);

        var isCurrentMarked = false;

        for (var i = 1; i <= termInMonths; i++)
        {
            var dueDate = startDate.AddMonths(i);
            var interestPortion = Math.Round(remainingBalance * monthlyRate, 2);
            var principalPortion = monthlyInstallment - interestPortion;

            if (i == termInMonths)
            {
                // Adjust final installment so remaining balance becomes exactly 0
                principalPortion = remainingBalance;
                monthlyInstallment = principalPortion + interestPortion;
            }

            remainingBalance -= principalPortion;

            if (remainingBalance < 0 || i == termInMonths)
            {
                remainingBalance = 0;
            }

            var row = new AmortizationRow
            {
                LoanId = loan.Id,
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalPortion = principalPortion,
                InterestPortion = interestPortion,
                RemainingBalance = remainingBalance,
                IsCurrent = false,
            };

            if (!isCurrentMarked && dueDate.Date >= DateTime.Today)
            {
                row.IsCurrent = true;
                isCurrentMarked = true;
            }

            rows.Add(row);
        }

        return rows;
    }
}