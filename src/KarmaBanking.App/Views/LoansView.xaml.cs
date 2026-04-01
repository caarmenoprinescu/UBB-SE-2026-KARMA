using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class LoansView : Page
    {
        private LoansViewModel _viewModel;

        public LoansView()
        {
            this.InitializeComponent();

            var repo = new LoanRepository();
            var service = new LoanService(repo);

            _viewModel = new LoansViewModel(service, repo);
            this.DataContext = _viewModel;

            _viewModel.loadLoans();
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int loanId = (int)button.Tag;

            _viewModel.LoadAmortization(loanId);
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int loanId = (int)button.Tag;

            _viewModel.PayLoan(loanId);
        }

        private void ApplyLoan_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ApplyLoanView));
        }
    }
}