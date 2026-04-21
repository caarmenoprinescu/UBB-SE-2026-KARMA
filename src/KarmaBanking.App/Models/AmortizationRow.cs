// <copyright file="AmortizationRow.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;

public class AmortizationRow
{
    public int Id { get; set; }

    public int LoanId { get; set; }

    public int InstallmentNumber { get; set; }

    public DateTime DueDate { get; set; }

    public decimal PrincipalPortion { get; set; }

    public decimal InterestPortion { get; set; }

    public decimal RemainingBalance { get; set; }

    public bool IsCurrent { get; set; }
}