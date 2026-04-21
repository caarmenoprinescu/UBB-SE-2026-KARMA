namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.Repositories;
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;

    public sealed partial class InvestmentsView : Page
    {
        public InvestmentsView()
        {
            InitializeComponent();

            ViewModel = new InvestmentsViewModel(new InvestmentRepository());
            DataContext = ViewModel;

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        public InvestmentsViewModel ViewModel { get; }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.EnsureInitialized();
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.StopMarketDataPolling();
            Loaded -= OnPageLoaded;
            Unloaded -= OnPageUnloaded;
        }
    }
}