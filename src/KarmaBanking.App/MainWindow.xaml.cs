using KarmaBanking.App.Views;
using Microsoft.UI.Xaml;

namespace KarmaBanking.App
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Activated += OnFirstActivated;
        }

        private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= OnFirstActivated;

            //MainFrame.Navigate(typeof(LoansView));

            MainFrame.Navigate(typeof(CryptoTradingView));

        }
    }
}
