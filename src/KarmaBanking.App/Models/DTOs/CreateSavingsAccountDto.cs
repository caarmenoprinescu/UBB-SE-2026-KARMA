// <copyright file="CreateSavingsAccountDto.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

using System;
using KarmaBanking.App.Models.Enums;

public class CreateSavingsAccountDto
{
    public int UserId { get; set; }

    public string SavingsType { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public decimal InitialDeposit { get; set; }

    public int FundingAccountId { get; set; }

    public decimal? TargetAmount { get; set; }

    public DateTime? TargetDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public DepositFrequency? DepositFrequency { get; set; }
}