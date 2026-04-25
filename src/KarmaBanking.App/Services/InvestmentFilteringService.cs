// <copyright file="InvestmentFilteringService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using KarmaBanking.App.Models;

public class InvestmentFilteringService
{
    public IEnumerable<InvestmentHolding> FilterHoldingsByAssetType(
        IEnumerable<InvestmentHolding> holdings,
        string filterType)
    {
        if (holdings == null)
        {
            return Enumerable.Empty<InvestmentHolding>();
        }

        return holdings.Where(holding => this.MatchesFilter(holding, filterType));
    }

    private bool MatchesFilter(InvestmentHolding holding, string filterType)
    {
        if (holding == null)
        {
            return false;
        }

        var assetType = holding.AssetType?.Trim() ?? string.Empty;

        return filterType switch
        {
            "Stocks" => assetType.Equals("Stock", StringComparison.OrdinalIgnoreCase)
                        || assetType.Equals("Stocks", StringComparison.OrdinalIgnoreCase),
            "ETFs" => assetType.Equals("ETF", StringComparison.OrdinalIgnoreCase)
                      || assetType.Equals("ETFs", StringComparison.OrdinalIgnoreCase),
            "Bonds" => assetType.Equals("Bond", StringComparison.OrdinalIgnoreCase)
                       || assetType.Equals("Bonds", StringComparison.OrdinalIgnoreCase),
            "Crypto" => assetType.Equals("Crypto", StringComparison.OrdinalIgnoreCase),
            "Other" => !assetType.Equals("Stock", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("Stocks", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("ETF", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("ETFs", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("Bond", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("Bonds", StringComparison.OrdinalIgnoreCase)
                       && !assetType.Equals("Crypto", StringComparison.OrdinalIgnoreCase),
            _ => true,
        };
    }
}