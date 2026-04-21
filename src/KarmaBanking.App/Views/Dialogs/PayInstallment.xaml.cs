using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;

namespace KarmaBanking.App.Views.Dialogs
{
    public sealed partial class PayInstallmentDialog : ContentDialog
    {

        private readonly LoansViewModel _viewModel;
        public PayInstallmentDialog(LoansViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            UpdatePreview();
        }

        private async void OnConfirmClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                await _viewModel.PayInstallmentAsync();
            }
            catch (Exception)
            {
                args.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void OnStandardChecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            if (CustomAmountPanel != null)
            {
                CustomAmountPanel.Visibility = Visibility.Collapsed;
                _viewModel.CustomAmount = null;
            }

            UpdatePreview();
        }

        private void OnCustomChecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            CustomAmountPanel.Visibility = Visibility.Visible;
            if (_viewModel.SelectedLoan != null)
            {
                if (!_viewModel.CustomAmount.HasValue)
                {
                    _viewModel.CustomAmount = (double)_viewModel.SelectedLoan.Loan.MonthlyInstallment;
                }

                if (_viewModel.CustomAmount > (double)_viewModel.SelectedLoan.Loan.OutstandingBalance)
                {
                    _viewModel.CustomAmount = (double)_viewModel.SelectedLoan.Loan.OutstandingBalance;
                }

                CustomAmountBox.Text = _viewModel.CustomAmount?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
            }

            UpdatePreview();
        }

        private void OnCustomAmountTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void OnCustomAmountLostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_viewModel == null)
            {
                return;
            }

            if (_viewModel.SelectedLoan == null)
            {
                BalanceAfterPaymentText.Text = string.Empty;
                RemainingTermAfterPaymentText.Text = string.Empty;
                return;
            }

            Loan loan = _viewModel.SelectedLoan.Loan;
            decimal paymentAmount = StandardRadio.IsChecked == true
                ? loan.MonthlyInstallment
                : GetCustomPaymentAmount();

            decimal balanceAfterPayment = Math.Max(0m, loan.OutstandingBalance - paymentAmount);

            int monthsPaid = StandardRadio.IsChecked == true
                ? 1
                : paymentAmount <= 0m
                    ? 0
                    : (int)Math.Floor(paymentAmount / loan.MonthlyInstallment);

            int remainingTerm = Math.Max(0, loan.RemainingMonths - monthsPaid);

            BalanceAfterPaymentText.Text = balanceAfterPayment.ToString("C2");
            RemainingTermAfterPaymentText.Text = $"{remainingTerm} mo";
        }

        private decimal GetCustomPaymentAmount()
        {
            if (CustomAmountBox != null)
            {
                if (string.IsNullOrWhiteSpace(CustomAmountBox.Text))
                {
                    _viewModel.CustomAmount = null;
                    return 0m;
                }

                if (decimal.TryParse(CustomAmountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal parsedCurrentCulture))
                {
                    _viewModel.CustomAmount = (double)parsedCurrentCulture;
                    return parsedCurrentCulture;
                }

                if (decimal.TryParse(CustomAmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsedInvariantCulture))
                {
                    _viewModel.CustomAmount = (double)parsedInvariantCulture;
                    return parsedInvariantCulture;
                }

                _viewModel.CustomAmount = null;
                return 0m;
            }

            _viewModel.CustomAmount = null;
            return 0m;
        }
    }
}
