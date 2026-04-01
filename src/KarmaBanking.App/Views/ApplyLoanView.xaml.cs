using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class ApplyLoanView : Page
    {
        public ApplyLoanView()
        {
            this.InitializeComponent();

            var repo = new LoanRepository();
            var service = new LoanService(repo);

            this.DataContext = new ApplyLoanViewModel(service);
        }

        private void Submit_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var vm = (ApplyLoanViewModel)this.DataContext;
            vm.Submit();
        }
    }
}