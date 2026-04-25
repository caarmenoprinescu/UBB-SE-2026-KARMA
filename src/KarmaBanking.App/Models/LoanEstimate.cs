// <copyright file="LoanEstimate.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

/// <summary>
/// Represents a preliminary quote computed for a loan request.
/// </summary>
public class LoanEstimate
{
    /// <summary>
    /// Gets or sets the indicative annual interest rate.
    /// </summary>
    public decimal IndicativeRate { get; set; }

    /// <summary>
    /// Gets or sets the projected monthly installment.
    /// </summary>
    public decimal MonthlyInstallment { get; set; }

    /// <summary>
    /// Gets or sets the estimated total amount repayable over the term.
    /// </summary>
    public decimal TotalRepayable { get; set; }
}