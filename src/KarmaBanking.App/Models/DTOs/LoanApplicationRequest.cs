// <copyright file="LoanApplicationRequest.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

public class LoanApplicationRequest
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public LoanType LoanType { get; set; }

    public decimal DesiredAmount { get; set; }

    public int PreferredTermMonths { get; set; }

    public string Purpose { get; set; }
}