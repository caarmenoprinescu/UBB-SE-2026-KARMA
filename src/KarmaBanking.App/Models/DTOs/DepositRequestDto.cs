// <copyright file="DepositRequestDto.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

public class DepositRequestDto
{
    public int AccountId { get; set; }

    public decimal Amount { get; set; }

    public string Source { get; set; } = string.Empty;
}