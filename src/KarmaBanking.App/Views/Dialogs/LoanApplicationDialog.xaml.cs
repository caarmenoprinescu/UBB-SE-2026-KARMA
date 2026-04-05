using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Views.Dialogs
{
    public sealed partial class LoanApplicationDialog : ContentDialog
    {

        private readonly LoansViewModel _viewModel;
        private bool _isReviewStage = false;

        public LoanApplicationDialog(LoansViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;


        }

        private async void OnSubmitClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            if (!_isReviewStage)
            {
                args.Cancel = true;

                FormPanel.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                ReviewPanel.Visibility = Microsoft.UI.Xaml.Visibility.Visible;

                sender.Title = "Application Review"
;
                sender.PrimaryButtonText = "Submit";
                _isReviewStage = true;
            }
            else
            {
                await _viewModel.ApplyForLoanAsync();

                if (!string.IsNullOrEmpty(_viewModel.ApplicationResult))
                {
                    ResultBar.Message = _viewModel.ApplicationResult;
                    ResultBar.Severity = _viewModel.ApplicationResult.Contains("approved")
                        ? InfoBarSeverity.Success
                        : InfoBarSeverity.Error;
                    ResultBar.IsOpen = true;
                    args.Cancel = true;

                }
            }
            deferral.Complete();

        }
    }
}
