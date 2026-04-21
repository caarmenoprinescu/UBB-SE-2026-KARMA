// <copyright file="LoanApplicationDialog.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views.Dialogs;

using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

public sealed partial class LoanApplicationDialog : ContentDialog
{
    private readonly LoansViewModel _viewModel;

    public LoanApplicationDialog(LoansViewModel viewModel)
    {
        this.InitializeComponent();
        this._viewModel = viewModel;
        this.DataContext = viewModel;
    }

    private async void OnSubmitClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (!this._viewModel.IsReviewVisible)
        {
            args.Cancel = true;
            this._viewModel.SwitchToReviewStage();
            sender.Title = this._viewModel.DialogTitle;
            sender.PrimaryButtonText = this._viewModel.DialogActionText;
        }
        else
        {
            await this._viewModel.ApplyForLoanAsync();

            if (!string.IsNullOrEmpty(this._viewModel.ApplicationResult))
            {
                this.ResultBar.Message = this._viewModel.ApplicationResult;
                this.ResultBar.Severity = this._viewModel.ApplicationWasApproved
                    ? InfoBarSeverity.Success
                    : InfoBarSeverity.Error;
                this.ResultBar.IsOpen = true;
                args.Cancel = true;
            }
        }

        deferral.Complete();
    }
}