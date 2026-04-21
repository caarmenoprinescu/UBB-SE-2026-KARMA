// <copyright file="PortfolioValuationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System.Linq;
using KarmaBanking.App.Models;

public class PortfolioValuationService
{
    public void UpdateHoldingValuation(InvestmentHolding holding, decimal updatedPrice)
    {
        holding.CurrentPrice = updatedPrice;
        holding.UnrealizedGainLoss = (holding.CurrentPrice - holding.AveragePurchasePrice) * holding.Quantity;
    }

    public void UpdatePortfolioTotals(Portfolio portfolio)
    {
        var holdings = portfolio.Holdings;
        portfolio.TotalValue = holdings.Sum(holding => holding.CurrentPrice * holding.Quantity);
        portfolio.TotalGainLoss = holdings.Sum(holding => holding.UnrealizedGainLoss);

        var totalCost = holdings.Sum(holding => holding.AveragePurchasePrice * holding.Quantity);
        portfolio.GainLossPercent = totalCost > 0 ? portfolio.TotalGainLoss / totalCost * 100 : 0;
    }
}