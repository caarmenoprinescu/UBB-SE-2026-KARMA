// <copyright file="LoanPresentationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using KarmaBanking.App.Utils;

public class LoanPresentationService
{
    public double GetRepaymentProgress(Loan loan)
    {
        return (double)AmortizationCalculator.ComputeRepaymentProgress(
            loan.Principal,
            loan.OutstandingBalance);
    }
}