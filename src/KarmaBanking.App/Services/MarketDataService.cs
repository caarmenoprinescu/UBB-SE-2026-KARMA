// <copyright file="MarketDataService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using KarmaBanking.App.Services.Interfaces;

public class MarketDataService : IMarketDataService
{
    private const int DefaultPollingIntervalInMilliseconds = 5000;
    private const double MaximumPriceFluctuationPercentage = 0.04;
    private const double PriceFluctuationOffset = 0.02;

    private readonly Dictionary<string, decimal> currentPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = 68000m,
        ["ETH"] = 3400m,
        ["AAPL"] = 185m,
        ["MSFT"] = 420m,
        ["GOOGL"] = 155m,
        ["TSLA"] = 650m,
        ["SPY"] = 520m
    };

    private readonly Random randomNumberGenerator = new();

    private readonly object synchronizationRoot = new();

    private Timer? pollingTimer;
    private Action? priceUpdateHandler;
    private List<string> trackedTickerSymbols = new();

    public void StartPolling(List<string> tickerSymbols)
    {
        lock (this.synchronizationRoot)
        {
            this.trackedTickerSymbols = tickerSymbols
                .Where(ticker => !string.IsNullOrWhiteSpace(ticker))
                .Select(ticker => ticker.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (this.pollingTimer != null)
            {
                return;
            }

            this.pollingTimer = new Timer(
                timerState =>
                {
                    lock (this.synchronizationRoot)
                    {
                        foreach (var ticker in this.trackedTickerSymbols)
                        {
                            if (!this.currentPrices.TryGetValue(ticker, out var currentPrice))
                            {
                                continue;
                            }

                            var changePercentage =
                                (decimal)((this.randomNumberGenerator.NextDouble() *
                                           MaximumPriceFluctuationPercentage) - PriceFluctuationOffset);
                            var updatedPrice = currentPrice * (1 + changePercentage);
                            this.currentPrices[ticker] = decimal.Round(updatedPrice, 2);
                        }
                    }

                    this.priceUpdateHandler?.Invoke();
                },
                null,
                DefaultPollingIntervalInMilliseconds,
                DefaultPollingIntervalInMilliseconds);
        }
    }

    public void StopPolling()
    {
        lock (this.synchronizationRoot)
        {
            this.pollingTimer?.Dispose();
            this.pollingTimer = null;
        }
    }

    public decimal GetPrice(string tickerSymbol)
    {
        if (string.IsNullOrWhiteSpace(tickerSymbol))
        {
            return 0m;
        }

        lock (this.synchronizationRoot)
        {
            return this.currentPrices.TryGetValue(tickerSymbol.Trim().ToUpperInvariant(), out var price) ? price : 0m;
        }
    }

    public void RegisterPriceUpdateHandler(Action updateHandler)
    {
        this.priceUpdateHandler = updateHandler;
    }
}