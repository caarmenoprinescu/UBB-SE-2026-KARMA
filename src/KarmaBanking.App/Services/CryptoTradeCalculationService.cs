// <copyright file="CryptoTradeCalculationService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Globalization;

public class CryptoTradeCalculationService
{
    private const decimal FeeRate = 0.015m;
    private const decimal MinimumFee = 0.50m;

    public bool TryParsePositiveQuantity(string quantityText, out decimal quantity)
{
    // Adding NumberStyles.Any and InvariantCulture ensures the dot is treated as a decimal
    if (decimal.TryParse(quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out quantity) && quantity > 0)
    {
        return true;
    }

    quantity = 0m;
    return false;
}

    public decimal GetMockMarketPrice(string ticker)
    {
        return ticker == "BTC" ? 65000m : 3000m;
    }

    public (decimal EstimatedFee, decimal TotalAmount) CalculateTradePreview(
        string ticker,
        string actionType,
        decimal quantity)
    {
        var tradeValue = quantity * this.GetMockMarketPrice(ticker);
        var calculatedFee = Math.Round(tradeValue * FeeRate, 2);
        var estimatedFee = calculatedFee < MinimumFee ? MinimumFee : calculatedFee;
        var totalAmount = actionType == "BUY"
            ? tradeValue + estimatedFee
            : tradeValue - estimatedFee;

        return (estimatedFee, totalAmount);
    }

    public bool CanExecuteTrade(
        bool isSubmitting,
        string quantityText,
        string actionType,
        decimal totalAmount,
        decimal currentBalance)
    {
        if (isSubmitting || !this.TryParsePositiveQuantity(quantityText, out _))
        {
            return false;
        }

        if (actionType == "BUY" && totalAmount > currentBalance)
        {
            return false;
        }

        return true;
    }
}