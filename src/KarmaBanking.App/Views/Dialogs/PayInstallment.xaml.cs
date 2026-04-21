using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

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
            UpdatePreviewUi();
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
            }

            _viewModel.SelectStandardPayment();
            UpdatePreviewUi();
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
                CustomAmountBox.Text = _viewModel.SelectCustomPayment();
            }

            UpdatePreviewUi();
        }

        private void OnCustomAmountTextChanged(object sender, TextChangedEventArgs e)
        {
            _viewModel.UpdateCustomPayment(CustomAmountBox.Text);
            UpdatePreviewUi();
        }

        private void OnCustomAmountLostFocus(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateCustomPayment(CustomAmountBox.Text);
            UpdatePreviewUi();
        }

        private void UpdatePreviewUi()
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

            _viewModel.UpdatePaymentPreview(StandardRadio.IsChecked == true, CustomAmountBox?.Text ?? string.Empty);
            BalanceAfterPaymentText.Text = _viewModel.PaymentPreviewBalance.ToString("C2");
            RemainingTermAfterPaymentText.Text = $"{_viewModel.PaymentPreviewRemainingMonths} mo";
        }
    }
}
