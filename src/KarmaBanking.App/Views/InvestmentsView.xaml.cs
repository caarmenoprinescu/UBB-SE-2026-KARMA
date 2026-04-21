namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.Repositories;
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Controls.Primitives;
    using System.Collections.Generic;
    using System.ComponentModel;

    public sealed partial class InvestmentsView : Page
    {
        private const string RefreshPricesEventName = "refreshPrices";
        private readonly List<ToggleButton> filterButtons;
        private bool hasPageLoaded;

        public InvestmentsView()
        {
            InitializeComponent();

            ViewModel = new InvestmentsViewModel(new InvestmentRepository());
            DataContext = ViewModel;

            filterButtons =
            [
                AllFilterButton,
                StocksFilterButton,
                EtfsFilterButton,
                BondsFilterButton,
                CryptoFilterButton,
                OtherFilterButton
            ];

            HoldingsListView.ItemsSource = ViewModel.DisplayedHoldings;

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
            ViewModel.LoadUserPortfolio();
            UpdateEmptyState();
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
            if (e.PropertyName == nameof(InvestmentsViewModel.UserPortfolio)
                || e.PropertyName == RefreshPricesEventName
                || e.PropertyName == nameof(InvestmentsViewModel.IsPortfolioLoading))
            {
                UpdateEmptyState();
            }
        }

        private void OnFilterClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton selectedButton)
            {
                foreach (ToggleButton button in filterButtons)
                {
                    button.IsChecked = button == selectedButton;
                }

                ViewModel.ApplyFilter(selectedButton.Tag?.ToString() ?? "All");
            }
        }

        private void UpdateEmptyState()
        {
            bool isEmpty = !ViewModel.IsPortfolioLoading && ViewModel.DisplayedHoldings.Count == 0;
            EmptyStateTextBlock.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            HoldingsListView.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}