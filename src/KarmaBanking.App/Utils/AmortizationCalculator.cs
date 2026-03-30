using System;
using System.Collections.Generic;
using KarmaBanking.App.Models;

public class AmortizationCalculator
{
    public LoanEstimate computeEstimate(decimal amount, decimal annualRate, int termMonths)
    {
        decimal monthlyRate = annualRate / 12m / 100m;
        decimal monthlyInstallment;

        if (monthlyRate == 0)
        {
            monthlyInstallment = amount / termMonths;
        }
        else
        {
            monthlyInstallment = amount * (monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, termMonths)) /
                                 ((decimal)Math.Pow(1 + (double)monthlyRate, termMonths) - 1);
        }

        monthlyInstallment = Math.Round(monthlyInstallment, 2);
        decimal totalRepayable = Math.Round(monthlyInstallment * termMonths, 2);

        return new LoanEstimate
        {
            IndicativeRate = annualRate,
            MonthlyInstallment = monthlyInstallment,
            TotalRepayable = totalRepayable
        };
    }

    public List<AmortizationRow> generate(Loan loan)
    {
        var rows = new List<AmortizationRow>();

        decimal principal = loan.principal;
        decimal annualRate = loan.interestRate;
        int termInMonths = loan.TermInMonths;
        DateTime startDate = loan.StartDate;

        decimal monthlyRate = annualRate / 12m / 100m;
        decimal remainingBalance = principal;
        decimal monthlyInstallment;

        if (monthlyRate == 0)
        {
            monthlyInstallment = remainingBalance / termInMonths;
        }
        else
        {
            monthlyInstallment = remainingBalance * (monthlyRate * (decimal)Math.Pow(1 + (double)monthlyRate, termInMonths)) /
                                 ((decimal)Math.Pow(1 + (double)monthlyRate, termInMonths) - 1);
        }

        monthlyInstallment = Math.Round(monthlyInstallment, 2);

        bool isCurrentMarked = false;

        for (int i = 1; i <= termInMonths; i++)
        {
            DateTime dueDate = startDate.AddMonths(i);
            decimal interestPortion = Math.Round(remainingBalance * monthlyRate, 2);
            decimal principalPortion = monthlyInstallment - interestPortion;

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
                LoanId = loan.id,
                InstallmentNumber = i,
                DueDate = dueDate,
                PrincipalPortion = principalPortion,
                InterestPortion = interestPortion,
                RemainingBalance = remainingBalance,
                IsCurrent = false
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

    public decimal computePenalty(SavingsAccount acc)
    {
        return 0;
    }
}
