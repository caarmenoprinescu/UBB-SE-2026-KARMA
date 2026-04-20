namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories;
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Controls.Primitives;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;

    public sealed partial class InvestmentsView : Page
    {
        // Redenumit pentru a fi semantic conform cerintei
        private const string RefreshPricesEventName = "refreshPrices";
        private readonly ObservableCollection<InvestmentHolding> displayedHoldings;
        private readonly List<ToggleButton> filterButtons;
        private bool hasPageLoaded;
        private string activeFilterType = "All";

        public InvestmentsView()
        {
            InitializeComponent();

            // Dependency Injection manual
            ViewModel = new InvestmentsViewModel(new InvestmentRepository());
            DataContext = ViewModel;

            displayedHoldings = [];
            filterButtons =
            [
                AllFilterButton,
                StocksFilterButton,
                EtfsFilterButton,
                BondsFilterButton,
                CryptoFilterButton,
                OtherFilterButton
            ];

            HoldingsListView.ItemsSource = displayedHoldings;

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        public InvestmentsViewModel ViewModel { get; }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (hasPageLoaded)
            {
                return;
            }

            hasPageLoaded = true;
            // Redenumit din loadPortfolio() -> LoadUserPortfolio()
            ViewModel.LoadUserPortfolio();
            RefreshDisplayedHoldings();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            // Redenumit din stopPolling() -> StopMarketDataPolling()
            ViewModel.StopMarketDataPolling();
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Loaded -= OnPageLoaded;
            Unloaded -= OnPageUnloaded;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Sincronizat cu noile nume de proprietati din ViewModel
            if (e.PropertyName == nameof(InvestmentsViewModel.UserPortfolio))
            {
                RefreshDisplayedHoldings();
            }
            else if (e.PropertyName == RefreshPricesEventName)
            {
                RefreshDisplayedHoldings();
            }
            else if (e.PropertyName == nameof(InvestmentsViewModel.IsPortfolioLoading))
            {
                UpdateEmptyState();
            }
        }

        private void OnFilterClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton selectedButton)
            {
                activeFilterType = selectedButton.Tag?.ToString() ?? "All";

                foreach (ToggleButton button in filterButtons)
                {
                    button.IsChecked = button == selectedButton;
                }

                RefreshDisplayedHoldings();
            }
        }

        private void RefreshDisplayedHoldings()
        {
            displayedHoldings.Clear();

            // Sincronizat: portfolio -> UserPortfolio
            IEnumerable<InvestmentHolding> holdings = ViewModel.UserPortfolio?.Holdings ?? Enumerable.Empty<InvestmentHolding>();
            foreach (InvestmentHolding holding in holdings.Where(MatchesActiveFilter))
            {
                displayedHoldings.Add(holding);
            }

            UpdateEmptyState();
        }

        private bool MatchesActiveFilter(InvestmentHolding holding)
        {
            string assetType = holding.AssetType?.Trim() ?? string.Empty;

            return activeFilterType switch
            {
                "Stocks" => assetType.Equals("Stock", System.StringComparison.OrdinalIgnoreCase)
                    || assetType.Equals("Stocks", System.StringComparison.OrdinalIgnoreCase),
                "ETFs" => assetType.Equals("ETF", System.StringComparison.OrdinalIgnoreCase)
                    || assetType.Equals("ETFs", System.StringComparison.OrdinalIgnoreCase),
                "Bonds" => assetType.Equals("Bond", System.StringComparison.OrdinalIgnoreCase)
                    || assetType.Equals("Bonds", System.StringComparison.OrdinalIgnoreCase),
                "Crypto" => assetType.Equals("Crypto", System.StringComparison.OrdinalIgnoreCase),
                "Other" => !assetType.Equals("Stock", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("Stocks", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("ETF", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("ETFs", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("Bond", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("Bonds", System.StringComparison.OrdinalIgnoreCase)
                    && !assetType.Equals("Crypto", System.StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        private void UpdateEmptyState()
        {
            // Sincronizat: isLoading -> IsPortfolioLoading
            bool isEmpty = !ViewModel.IsPortfolioLoading && displayedHoldings.Count == 0;
            EmptyStateTextBlock.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            HoldingsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}