// <copyright file="WithdrawResponseDto.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

using System;

public class WithdrawResponseDto
{
    public bool Success { get; set; }

    public decimal AmountWithdrawn { get; set; }

    public decimal PenaltyApplied { get; set; }

    public decimal NewBalance { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}