using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (CustomAmountPanel != null)
            {
                CustomAmountPanel.Visibility = Visibility.Collapsed;
                _viewModel.CustomAmount = null;
            }
        }

        private void OnCustomChecked(object sender, RoutedEventArgs e)
        {
            CustomAmountPanel.Visibility = Visibility.Visible;
            if (_viewModel.SelectedLoan != null)
            {
                if (!_viewModel.CustomAmount.HasValue)
                    _viewModel.CustomAmount = (double)_viewModel.SelectedLoan.Loan.MonthlyInstallment;

                if (_viewModel.CustomAmount > (double)_viewModel.SelectedLoan.Loan.OutstandingBalance)
                    _viewModel.CustomAmount = (double)_viewModel.SelectedLoan.Loan.OutstandingBalance;
            }
        }
    }
}
