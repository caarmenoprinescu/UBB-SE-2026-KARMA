// <copyright file="ClosureResultDTO.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

using System;

public class ClosureResultDto
{
    public bool Success { get; set; }

    public decimal TransferredAmount { get; set; }

    public decimal PenaltyApplied { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime ClosedAt { get; set; }
}