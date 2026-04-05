using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services;
using Microsoft.UI.Dispatching;

namespace KarmaBanking.App.ViewModels
{
    public class InvestmentsViewModel : INotifyPropertyChanged
    {
        private const string RefreshPricesEvent = "refreshPrices";
        private readonly IInvestmentRepository _repo;
        private readonly MarketDataService _marketData;
        private readonly DispatcherQueue? _dispatcherQueue;

        private Portfolio _portfolio;
        private bool _isLoading;

        public InvestmentsViewModel(IInvestmentRepository repo)
        {
            _repo = repo;
            _marketData = new MarketDataService();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _marketData.onPriceUpdate(refreshPrices);
            _portfolio = new Portfolio();
        }

        public Portfolio portfolio
        {
            get => _portfolio;
            set
            {
                _portfolio = value;
                OnPropertyChanged();
            }
        }

        public bool isLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public void loadPortfolio()
        {
            isLoading = true;

            try
            {
                portfolio = _repo.GetPortfolio(1);
                _marketData.startPolling(portfolio.Holdings.Select(holding => holding.Ticker).ToList());

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"loadPortfolio error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        public void refreshPrices()
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(refreshPrices);
                return;
            }

            if (portfolio?.Holdings == null || portfolio.Holdings.Count == 0)
                return;

            foreach (var holding in portfolio.Holdings)
            {
                decimal updatedPrice = _marketData.getPrice(holding.Ticker);
                if (updatedPrice <= 0) continue;

                holding.CurrentPrice = updatedPrice;
                holding.UnrealizedGainLoss =
                    (holding.CurrentPrice - holding.AvgPurchasePrice) * holding.Quantity;
            }

            portfolio.TotalValue = portfolio.Holdings.Sum(h => h.CurrentPrice * h.Quantity);
            portfolio.TotalGainLoss = portfolio.Holdings.Sum(h => h.UnrealizedGainLoss);

            decimal totalCost = portfolio.Holdings.Sum(h => h.AvgPurchasePrice * h.Quantity);
            portfolio.GainLossPercent = totalCost > 0
                ? (portfolio.TotalGainLoss / totalCost) * 100
                : 0;

            OnPropertyChanged(nameof(portfolio));
            OnPropertyChanged(RefreshPricesEvent);
        }

        public void stopPolling()
        {
            _marketData.stopPolling();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
