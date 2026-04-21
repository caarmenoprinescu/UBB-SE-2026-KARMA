using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views.Dialogs
{
    public sealed partial class LoanApplicationDialog : ContentDialog
    {
        private readonly LoansViewModel _viewModel;

        public LoanApplicationDialog(LoansViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
        }

        private async void OnSubmitClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            if (!_viewModel.IsReviewVisible)
            {
                args.Cancel = true;
                _viewModel.SwitchToReviewStage();
                sender.Title = _viewModel.DialogTitle;
                sender.PrimaryButtonText = _viewModel.DialogActionText;
            }
            else
            {
                await _viewModel.ApplyForLoanAsync();

                if (!string.IsNullOrEmpty(_viewModel.ApplicationResult))
                {
                    ResultBar.Message = _viewModel.ApplicationResult;
                    ResultBar.Severity = _viewModel.ApplicationWasApproved
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
