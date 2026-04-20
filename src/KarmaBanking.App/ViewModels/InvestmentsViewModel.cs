namespace KarmaBanking.App.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Services.Interfaces; // Ensure this is present
    using Microsoft.UI.Dispatching;

    public class InvestmentsViewModel : INotifyPropertyChanged
    {
        private const string RefreshPricesEventName = "refreshPrices";
        private readonly IInvestmentRepository investmentRepository;
        private readonly IMarketDataService marketDataService; // Changed to Interface
        private readonly DispatcherQueue? dispatcherQueue;

        private Portfolio userPortfolio;
        private bool isPortfolioLoading;

        public InvestmentsViewModel(IInvestmentRepository investmentRepository)
        {
            this.investmentRepository = investmentRepository;
            marketDataService = new MarketDataService();
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Corrected: RegisterPriceUpdateHandler
            marketDataService.RegisterPriceUpdateHandler(RefreshHoldingPrices);
            userPortfolio = new Portfolio();
        }

        public Portfolio UserPortfolio
        {
            get => userPortfolio;
            set
            {
                userPortfolio = value;
                OnPropertyChanged();
            }
        }

        public bool IsPortfolioLoading
        {
            get => isPortfolioLoading;
            set
            {
                isPortfolioLoading = value;
                OnPropertyChanged();
            }
        }

        public void LoadUserPortfolio()
        {
            IsPortfolioLoading = true;

            try
            {
                UserPortfolio = investmentRepository.GetPortfolio(1);

                // Corrected: StartPolling
                marketDataService.StartPolling(UserPortfolio.Holdings.Select(holding => holding.Ticker).ToList());
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"LoadUserPortfolio error: {exception.Message}");
            }
            finally
            {
                IsPortfolioLoading = false;
            }
        }

        public void RefreshHoldingPrices()
        {
            if (dispatcherQueue != null && !dispatcherQueue.HasThreadAccess)
            {
                dispatcherQueue.TryEnqueue(RefreshHoldingPrices);
                return;
            }

            if (UserPortfolio?.Holdings == null || UserPortfolio.Holdings.Count == 0)
            {
                return;
            }

            foreach (var holding in UserPortfolio.Holdings)
            {
                // Corrected: GetPrice
                decimal updatedPrice = marketDataService.GetPrice(holding.Ticker);
                if (updatedPrice <= 0)
                {
                    continue;
                }

                holding.CurrentPrice = updatedPrice;
                holding.UnrealizedGainLoss = (holding.CurrentPrice - holding.AveragePurchasePrice) * holding.Quantity;
            }

            UserPortfolio.TotalValue = UserPortfolio.Holdings.Sum(holding => holding.CurrentPrice * holding.Quantity);
            UserPortfolio.TotalGainLoss = UserPortfolio.Holdings.Sum(holding => holding.UnrealizedGainLoss);

            decimal totalCost = UserPortfolio.Holdings.Sum(holding => holding.AveragePurchasePrice * holding.Quantity);
            UserPortfolio.GainLossPercent = totalCost > 0 ? (UserPortfolio.TotalGainLoss / totalCost) * 100 : 0;

            OnPropertyChanged(nameof(UserPortfolio));
            OnPropertyChanged(RefreshPricesEventName);
        }

        public void StopMarketDataPolling()
        {
            // Corrected: StopPolling
            marketDataService.StopPolling();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}