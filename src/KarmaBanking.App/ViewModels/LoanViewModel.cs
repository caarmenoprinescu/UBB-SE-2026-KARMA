// <copyright file="LoanViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class LoanViewModel : ObservableObject
{
    public LoanViewModel(Loan loan, double repaymentProgress)
    {
        this.Loan = loan;
        this.RepaymentProgress = repaymentProgress;
    }

    public Loan Loan { get; }

    public double RepaymentProgress { get; }

    public int PaidInstallments => this.Loan.TermInMonths - this.Loan.RemainingMonths;
}