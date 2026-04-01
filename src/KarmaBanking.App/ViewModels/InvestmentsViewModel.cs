<<<<<<< HEAD
using System;
=======
<<<<<<< HEAD
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services;
using Microsoft.UI.Dispatching;
=======
>>>>>>> dd13b5775c0b4c9716c5375638e7037a28c1a5d9
using System.ComponentModel;
using System.Runtime.CompilerServices;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
>>>>>>> main

namespace KarmaBanking.App.ViewModels
{
    public class InvestmentsViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentRepository _repo;
<<<<<<< HEAD
        private readonly MarketDataService _marketData;
        private readonly DispatcherQueue? _dispatcherQueue;
=======
>>>>>>> main

        public InvestmentsViewModel(IInvestmentRepository repo)
        {
            _repo = repo;
<<<<<<< HEAD
            _marketData = new MarketDataService();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _marketData.onPriceUpdate(refreshPrices);
=======
>>>>>>> main
            portfolio = new Portfolio();
        }

        private Portfolio _portfolio;
        public Portfolio portfolio
<<<<<<< HEAD
         {
                   get => _portfolio;
                   set { _portfolio = value; OnPropertyChanged(); }
         }
=======
           {
               get => _portfolio;
               set
                    {
                       _portfolio = value;
                       OnPropertyChanged();
                    }
            }
>>>>>>> main

        private bool _isLoading;
        public bool isLoading
        {
<<<<<<< HEAD
                  get => _isLoading;
                  set { _isLoading = value; OnPropertyChanged(); }
=======
               get => _isLoading;
               set
                {
                   _isLoading = value;
                   OnPropertyChanged();
                 }
>>>>>>> main
        }

        public void loadPortfolio()
        {
            isLoading = true;
           

            try
            {
                portfolio = _repo.GetPortfolio(1);
               
<<<<<<< HEAD
                _marketData.startPolling(portfolio.Holdings.Select(holding => holding.Ticker).ToList());
            }
           catch (Exception ex)
            {
                  System.Diagnostics.Debug.WriteLine($"loadPortfolio error: {ex.Message}");
            }
=======
            }
            catch (Exception ex)
             {
                System.Diagnostics.Debug.WriteLine($"loadPortfolio error: {ex.Message}");
             }
>>>>>>> main
            finally
            {
                isLoading = false;
                
            }
        }

<<<<<<< HEAD
        public void refreshPrices()
        {
            if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
            {
                _dispatcherQueue.TryEnqueue(refreshPrices);
                return;
            }

            if (portfolio?.Holdings == null || portfolio.Holdings.Count == 0)
            {
                return;
            }

            foreach (InvestmentHolding holding in portfolio.Holdings)
            {
                decimal updatedPrice = _marketData.getPrice(holding.Ticker);
                if (updatedPrice <= 0)
                {
                    continue;
                }

                holding.CurrentPrice = updatedPrice;
                holding.UnrealizedGainLoss = (holding.CurrentPrice - holding.AvgPurchasePrice) * holding.Quantity;
            }

            portfolio.TotalValue = portfolio.Holdings.Sum(holding => holding.CurrentPrice * holding.Quantity);
            portfolio.TotalGainLoss = portfolio.Holdings.Sum(holding => holding.UnrealizedGainLoss);

            decimal totalCostBasis = portfolio.Holdings.Sum(holding => holding.AvgPurchasePrice * holding.Quantity);
            portfolio.GainLossPercent = totalCostBasis > 0
                ? (portfolio.TotalGainLoss / totalCostBasis) * 100
                : 0;

            OnPropertyChanged(nameof(portfolio));
        }

        public void stopPolling()
        {
            _marketData.stopPolling();
        }

=======
>>>>>>> main
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
