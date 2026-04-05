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

namespace KarmaBanking.App.Views
{
    public sealed partial class InvestmentsView : Page
    {
        private const string RefreshPricesEvent = "refreshPrices";
        private readonly ObservableCollection<InvestmentHolding> _displayedHoldings;
        private readonly List<ToggleButton> _filterButtons;
        private InvestmentsViewModel ViewModel { get; }
        private string _activeFilter = "All";
        private bool _hasLoaded;

        public InvestmentsView()
        {
            InitializeComponent();

            ViewModel = new InvestmentsViewModel(new InvestmentRepository());
            DataContext = ViewModel;

            _displayedHoldings = new ObservableCollection<InvestmentHolding>();
            _filterButtons = new List<ToggleButton>
            {
                AllFilterButton,
                StocksFilterButton,
                EtfsFilterButton,
                BondsFilterButton,
                CryptoFilterButton,
                OtherFilterButton
            };

            HoldingsListView.ItemsSource = _displayedHoldings;

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (_hasLoaded)
            {
                return;
            }

            _hasLoaded = true;
            ViewModel.loadPortfolio();
            RefreshDisplayedHoldings();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.stopPolling();
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            Loaded -= OnPageLoaded;
            Unloaded -= OnPageUnloaded;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvestmentsViewModel.portfolio))
            {
                RefreshDisplayedHoldings();
            }
            else if (e.PropertyName == RefreshPricesEvent)
            {
                RefreshDisplayedHoldings();
            }
            else if (e.PropertyName == nameof(InvestmentsViewModel.isLoading))
            {
                UpdateEmptyState();
            }
        }

        private void OnFilterClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton selectedButton)
            {
                _activeFilter = selectedButton.Tag?.ToString() ?? "All";

                foreach (ToggleButton button in _filterButtons)
                {
                    button.IsChecked = button == selectedButton;
                }

                RefreshDisplayedHoldings();
            }
        }

        private void RefreshDisplayedHoldings()
        {
            _displayedHoldings.Clear();

            IEnumerable<InvestmentHolding> holdings = ViewModel.portfolio?.Holdings ?? Enumerable.Empty<InvestmentHolding>();
            foreach (InvestmentHolding holding in holdings.Where(MatchesActiveFilter))
            {
                _displayedHoldings.Add(holding);
            }

            UpdateEmptyState();
        }

        private bool MatchesActiveFilter(InvestmentHolding holding)
        {
            string assetType = holding.AssetType?.Trim() ?? string.Empty;

            return _activeFilter switch
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
            bool isEmpty = !ViewModel.isLoading && _displayedHoldings.Count == 0;
            EmptyStateTextBlock.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            HoldingsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
