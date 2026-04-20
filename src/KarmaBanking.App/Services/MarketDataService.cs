namespace KarmaBanking.App.Services
{
    using KarmaBanking.App.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class MarketDataService : IMarketDataService
    {
        private const int DefaultPollingIntervalInMilliseconds = 5000;
        private const double MaximumPriceFluctuationPercentage = 0.04;
        private const double PriceFluctuationOffset = 0.02;

        private readonly Dictionary<string, decimal> currentPrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"] = 68000m,
            ["ETH"] = 3400m,
            ["AAPL"] = 185m,
            ["MSFT"] = 420m,
            ["GOOGL"] = 155m,
            ["TSLA"] = 650m,
            ["SPY"] = 520m
        };

        private readonly object synchronizationRoot = new object();
        private readonly Random randomNumberGenerator = new Random();

        private Timer? pollingTimer;
        private List<string> trackedTickerSymbols = new List<string>();
        private Action? priceUpdateHandler;

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

                this.pollingTimer = new Timer(timerState =>
                {
                    lock (this.synchronizationRoot)
                    {
                        foreach (string ticker in this.trackedTickerSymbols)
                        {
                            if (!this.currentPrices.TryGetValue(ticker, out decimal currentPrice))
                            {
                                continue;
                            }

                            decimal changePercentage = (decimal)((this.randomNumberGenerator.NextDouble() * MaximumPriceFluctuationPercentage) - PriceFluctuationOffset);
                            decimal updatedPrice = currentPrice * (1 + changePercentage);
                            this.currentPrices[ticker] = decimal.Round(updatedPrice, 2);
                        }
                    }

                    this.priceUpdateHandler?.Invoke();
                }, null, DefaultPollingIntervalInMilliseconds, DefaultPollingIntervalInMilliseconds);
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
                return this.currentPrices.TryGetValue(tickerSymbol.Trim().ToUpperInvariant(), out decimal price) ? price : 0m;
            }
        }

        public void RegisterPriceUpdateHandler(Action updateHandler)
        {
            this.priceUpdateHandler = updateHandler;
        }
    }
}