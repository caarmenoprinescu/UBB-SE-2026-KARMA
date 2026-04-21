// <copyright file="DepositResponseDto.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

using System;

public class DepositResponseDto
{
    public decimal NewBalance { get; set; }

    public int TransactionId { get; set; }

    public DateTime Timestamp { get; set; }
}