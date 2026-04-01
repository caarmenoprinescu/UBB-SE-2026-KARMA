using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class CryptoTradingView : Page
    {
        public CryptoTradingViewModel ViewModel { get; }

        public CryptoTradingView()
        {
            this.InitializeComponent();

            var repository = new InvestmentRepository();
            var service = new InvestmentService(repository);

            ViewModel = new CryptoTradingViewModel(service);
            DataContext = ViewModel;
        }

        private void OnActionTypeChecked(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            if (sender is RadioButton checkedRadioButton)
            {
                ViewModel.ActionType = checkedRadioButton.Content.ToString()?.ToUpper() ?? "BUY";
            }
        }
    }
}