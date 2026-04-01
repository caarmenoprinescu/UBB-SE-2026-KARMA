using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class ApplyLoanView : Page
    {
        private ApplyLoanViewModel _viewModel;

        public ApplyLoanView()
        {
            this.InitializeComponent();

            var repo = new LoanRepository();
            var service = new LoanService(repo);

            _viewModel = new ApplyLoanViewModel(service);
            this.DataContext = _viewModel;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Submit();
        }
    }
}