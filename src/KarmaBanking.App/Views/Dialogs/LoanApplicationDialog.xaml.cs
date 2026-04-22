// <copyright file="LoanApplicationDialog.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views.Dialogs;

using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

public sealed partial class LoanApplicationDialog : ContentDialog
{
    private readonly LoansViewModel viewModel;

    public LoanApplicationDialog(LoansViewModel viewModel)
    {
        this.InitializeComponent();
        this.viewModel = viewModel;
        this.DataContext = viewModel;
    }

    private async void OnSubmitClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        if (!this.viewModel.IsReviewVisible)
        {
            args.Cancel = true;
            this.viewModel.SwitchToReviewStage();
            sender.Title = this.viewModel.DialogTitle;
            sender.PrimaryButtonText = this.viewModel.DialogActionText;
        }
        else
        {
            await this.viewModel.ApplyForLoanAsync();

            if (!string.IsNullOrEmpty(this.viewModel.ApplicationResult))
            {
                this.ResultBar.Message = this.viewModel.ApplicationResult;
                this.ResultBar.Severity = this.viewModel.ApplicationWasApproved
                    ? InfoBarSeverity.Success
                    : InfoBarSeverity.Error;
                this.ResultBar.IsOpen = true;
                args.Cancel = true;
            }
        }

        deferral.Complete();
    }
}