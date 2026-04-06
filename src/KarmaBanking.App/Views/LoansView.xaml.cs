using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using KarmaBanking.App.Views.Dialogs;
using System;

namespace KarmaBanking.App.Views
{
    public sealed partial class LoansView : Page
    {
        private readonly LoansViewModel _viewModel;

        public LoansView()
        {

            InitializeComponent();
            _viewModel = new LoansViewModel();
            DataContext = _viewModel;

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await _viewModel.LoadLoansAsync();
        }


        private async void OnApplyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new LoanApplicationDialog(_viewModel)
                {
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async void OnPayClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is LoanViewModel loan)
                {
                    _viewModel.SelectedLoan = loan;
                    var dialog = new PayInstallmentDialog(_viewModel)
                    {
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }

        private async void OnScheduleClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is LoanViewModel loan)
                {
                    _viewModel.SelectedLoan = loan;
                    await _viewModel.LoadAmortizationAsync();
                    Frame.Navigate(typeof(AmortizationScheduleView), loan.Loan);

                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

        }
        private void OnFilterAll(object sender, RoutedEventArgs e)
            => _viewModel.StatusFilter = null;

        private void OnFilterActive(object sender, RoutedEventArgs e)
            => _viewModel.StatusFilter = LoanStatus.Active;

        private void OnFilterClosed(object sender, RoutedEventArgs e)
            => _viewModel.StatusFilter = LoanStatus.Passed;

        private void OnTypeFilterAll(object sender, RoutedEventArgs e)
            => _viewModel.TypeFilter = null;

        private void OnTypeFilterPersonal(object sender, RoutedEventArgs e)
            => _viewModel.TypeFilter = LoanType.Personal;

        private void OnTypeFilterMortgage(object sender, RoutedEventArgs e)
            => _viewModel.TypeFilter = LoanType.Mortgage;

        private void OnTypeFilterStudent(object sender, RoutedEventArgs e)
            => _viewModel.TypeFilter = LoanType.Student;

        private void OnTypeFilterAuto(object sender, RoutedEventArgs e)
            => _viewModel.TypeFilter = LoanType.Auto;
    }
}
