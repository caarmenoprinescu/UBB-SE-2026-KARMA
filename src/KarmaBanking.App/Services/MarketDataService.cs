using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace KarmaBanking.App.Services
{
    public class MarketDataService
    {
        private readonly Dictionary<string, decimal> _prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"] = 68000m,
            ["ETH"] = 3400m,
            ["AAPL"] = 185m,
            ["MSFT"] = 420m,
            ["GOOGL"] = 155m,
            ["SPY"] = 520m
        };

        private readonly object _syncRoot = new object();
        private readonly Random _random = new Random();
        private readonly int _pollIntervalMs = 5000;

        private Timer? _timer;
        private List<string> _trackedTickers = new List<string>();
        private Action? _priceUpdateHandler;

        public void startPolling(List<string> tickers)
        {
            lock (_syncRoot)
            {
                _trackedTickers = tickers
                    .Where(ticker => !string.IsNullOrWhiteSpace(ticker))
                    .Select(ticker => ticker.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();

                if (_timer != null)
                {
                    return;
                }

                _timer = new Timer(_ =>
                {
                    lock (_syncRoot)
                    {
                        foreach (string ticker in _trackedTickers)
                        {
                            if (!_prices.TryGetValue(ticker, out decimal currentPrice))
                            {
                                continue;
                            }

                            decimal changePercent = (decimal)(_random.NextDouble() * 0.04 - 0.02);
                            decimal updatedPrice = currentPrice * (1 + changePercent);
                            _prices[ticker] = decimal.Round(updatedPrice, 2);
                        }
                    }

                    _priceUpdateHandler?.Invoke();
                }, null, _pollIntervalMs, _pollIntervalMs);
            }
        }

        public void stopPolling()
        {
            lock (_syncRoot)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }

        public decimal getPrice(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return 0m;
            }

            lock (_syncRoot)
            {
                return _prices.TryGetValue(ticker.Trim().ToUpperInvariant(), out decimal price) ? price : 0m;
            }
        }

        public void onPriceUpdate(Action handler)
        {
            _priceUpdateHandler = handler;
        }
    }
}
