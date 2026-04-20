namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.Repositories;
    using KarmaBanking.App.Services;
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;

    public sealed partial class CryptoTradingView : Page
    {
        public CryptoTradingView()
        {
            InitializeComponent();

            // Dependency Injection manual pentru conformitate cu cerinta de separare a straturilor
            var investmentRepository = new InvestmentRepository();
            var investmentService = new InvestmentService(investmentRepository);

            ViewModel = new CryptoTradingViewModel(investmentService);
            DataContext = ViewModel;
        }

        public CryptoTradingViewModel ViewModel { get; }

        private void OnActionTypeChecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (sender is RadioButton checkedRadioButton)
            {
                ViewModel.ActionType = checkedRadioButton.Content.ToString()?.ToUpper() ?? "BUY";
            }
        }
    }
}