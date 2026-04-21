namespace KarmaBanking.App.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.Services.Interfaces; // Ensure this is present
    using KarmaBanking.App.Utils;
    using Microsoft.UI.Dispatching;

    public class InvestmentsViewModel : INotifyPropertyChanged
    {
        private const string RefreshPricesEventName = "refreshPrices";
        private readonly IInvestmentRepository investmentRepository;
        private readonly IMarketDataService marketDataService; // Changed to Interface
        private readonly DispatcherQueue? dispatcherQueue;
        private readonly InvestmentFilteringService filteringService;

        private Portfolio userPortfolio;
        private bool isPortfolioLoading;
        private string activeFilterType = "All";
        private ObservableCollection<InvestmentHolding> displayedHoldings;
        private bool hasLoaded;

        public InvestmentsViewModel(IInvestmentRepository investmentRepository)
        {
            this.investmentRepository = investmentRepository;
            marketDataService = new MarketDataService();
            filteringService = new InvestmentFilteringService();
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            SelectFilterCommand = new RelayCommand<string>(ApplyFilter);

            // Corrected: RegisterPriceUpdateHandler
            marketDataService.RegisterPriceUpdateHandler(RefreshHoldingPrices);
            userPortfolio = new Portfolio();
            displayedHoldings = [];
        }

        public InvestmentsViewModel()
            : this(new InvestmentRepository())
        {
        }

        public string ActiveFilterType
        {
            get => activeFilterType;
            set
            {
                if (activeFilterType == value)
                {
                    return;
                }

                activeFilterType = value;
                RefreshDisplayedHoldings();
                OnPropertyChanged();
            }
        }

        public ICommand SelectFilterCommand { get; }

        public bool IsEmptyStateVisible => !IsPortfolioLoading && DisplayedHoldings.Count == 0;

        public bool IsHoldingsVisible => !IsEmptyStateVisible;

        public ObservableCollection<InvestmentHolding> DisplayedHoldings
        {
            get => displayedHoldings;
            private set
            {
                displayedHoldings = value;
                OnPropertyChanged();
                NotifyEmptyStateChanged();
            }
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
                NotifyEmptyStateChanged();
            }
        }

        public void EnsureInitialized()
        {
            if (hasLoaded)
            {
                return;
            }

            hasLoaded = true;
            LoadUserPortfolio();
        }

        public void LoadUserPortfolio()
        {
            IsPortfolioLoading = true;

            try
            {
                UserPortfolio = investmentRepository.GetPortfolio(1);
                RefreshDisplayedHoldings();

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

            RefreshDisplayedHoldings();
            OnPropertyChanged(nameof(UserPortfolio));
            OnPropertyChanged(RefreshPricesEventName);
        }

        public void ApplyFilter(string? filterType)
        {
            ActiveFilterType = string.IsNullOrWhiteSpace(filterType) ? "All" : filterType;
        }

        private void RefreshDisplayedHoldings()
        {
            DisplayedHoldings.Clear();
            var holdings = UserPortfolio?.Holdings ?? Enumerable.Empty<InvestmentHolding>();
            var filteredHoldings = filteringService.FilterHoldingsByAssetType(holdings, ActiveFilterType);
            foreach (var holding in filteredHoldings)
            {
                DisplayedHoldings.Add(holding);
            }

            NotifyEmptyStateChanged();
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

        private void NotifyEmptyStateChanged()
        {
            OnPropertyChanged(nameof(IsEmptyStateVisible));
            OnPropertyChanged(nameof(IsHoldingsVisible));
        }
    }
}