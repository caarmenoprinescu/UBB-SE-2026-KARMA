// <copyright file="AmortizationScheduleView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using KarmaBanking.App.Utils;
using KarmaBanking.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

public sealed partial class AmortizationScheduleView : Page
{
    private Loan? loan;

    public AmortizationScheduleView()
    {
        this.InitializeComponent();

        this.ViewModel = new LoansViewModel();

        this.DataContext = this.ViewModel;

        // Highlight the current installment row after containers are created.
        this.AmortizationListView.ContainerContentChanging += this.OnRowContainerContentChanging;
    }

    private LoansViewModel ViewModel { get; }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is Loan loan)
        {
            this.loan = loan;
            this.PopulateStaticLabels(loan);

            this.ViewModel.SelectedLoan = new LoanViewModel(loan, this.GetRepaymentProgress(loan));
            await this.ViewModel.LoadAmortizationAsync();
        }
    }

    private void PopulateStaticLabels(Loan loan)
    {
        this.LoanSubHeaderText.Text =
            $"{loan.LoanType} · {loan.TermInMonths} months · {loan.InterestRate:0.##}%";

        var loanViewModel = new LoanViewModel(loan, this.GetRepaymentProgress(loan));
        this.TotalInstallmentsText.Text = loan.TermInMonths.ToString();
        this.PaidInstallmentsText.Text = loanViewModel.PaidInstallments.ToString();
        this.RemainingInstallmentsText.Text = loan.RemainingMonths.ToString();
    }

    private double GetRepaymentProgress(Loan loan)
    {
        return (double)AmortizationCalculator.ComputeRepaymentProgress(loan.Principal, loan.OutstandingBalance);
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
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    private async void OnDownloadPdfClicked(object sender, RoutedEventArgs e)
    {
        if (this.loan != null)
        {
            await this.ViewModel.DownloadSchedulePdfAsync();
        }
    }
}