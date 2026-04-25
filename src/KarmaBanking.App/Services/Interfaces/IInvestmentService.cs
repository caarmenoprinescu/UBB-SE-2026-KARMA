// <copyright file="IInvestmentService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

public interface IInvestmentService
{
    Task<bool> ExecuteCryptoTradeAsync(
        int portfolioIdentificationNumber,
        string ticker,
        string actionType,
        decimal quantity,
        decimal pricePerUnit);

    Portfolio GetPortfolio(int userIdentificationNumber);

    Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(
        int portfolioIdentificationNumber,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? ticker = null);
}