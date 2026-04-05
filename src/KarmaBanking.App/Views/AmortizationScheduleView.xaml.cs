using KarmaBanking.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace KarmaBanking.App.Views
{
    public sealed partial class AmortizationScheduleView : Page
    {
        private LoansViewModel ViewModel { get; }
        private Loan? _loan;

        public AmortizationScheduleView()
        {
            InitializeComponent();

            ViewModel = new LoansViewModel();

            DataContext = ViewModel;

            // Highlight the current installment row after containers are created.
            AmortizationListView.ContainerContentChanging += OnRowContainerContentChanging;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Loan loan)
            {
                _loan = loan;
                PopulateStaticLabels(loan);

                ViewModel.SelectedLoan = new LoanViewModel(loan);
                await ViewModel.LoadAmortizationAsync();
            }
        }

        private void PopulateStaticLabels(Loan loan)
        {
            LoanSubHeaderText.Text =
                $"{loan.LoanType} · {loan.TermInMonths} months · {loan.InterestRate:0.##}%";

            int paid = loan.TermInMonths - loan.RemainingMonths;
            TotalInstallmentsText.Text = loan.TermInMonths.ToString();
            PaidInstallmentsText.Text = paid.ToString();
            RemainingInstallmentsText.Text = loan.RemainingMonths.ToString();
        }

        private void OnRowContainerContentChanging(
            ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.Item is AmortizationRow row && args.ItemContainer is ListViewItem container)
            {
                container.Background = row.IsCurrent
                    ? new SolidColorBrush(ColorHelper.FromArgb(40, 0, 120, 215))
                    : null;
            }
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void OnDownloadPdfClicked(object sender, RoutedEventArgs e)
        {
            if (_loan != null)
            {
               await ViewModel.DownloadSchedulePdfAsync();
            }
        }
    }
}
