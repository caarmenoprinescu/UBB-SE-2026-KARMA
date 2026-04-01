using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class InvestmentLogsView : Page
    {
        public InvestmentLogsViewModel ViewModel { get; }

        public InvestmentLogsView()
        {
            this.InitializeComponent();

            var repository = new InvestmentRepository();
            var service = new InvestmentService(repository);

            ViewModel = new InvestmentLogsViewModel(service);
            DataContext = ViewModel;

            // Load logs initially with no filters applied
            _ = ViewModel.LoadLogsAsync();
        }
    }
}