// <copyright file="PayInstallment.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views.Dialogs;

using System;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class PayInstallmentDialog : ContentDialog
{
    private readonly LoansViewModel _viewModel;

    public PayInstallmentDialog(LoansViewModel viewModel)
    {
        this.InitializeComponent();
        this._viewModel = viewModel;
        this.DataContext = viewModel;
        this.UpdatePreview();
    }

    private async void OnConfirmClicked(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            await this._viewModel.PayInstallmentAsync();
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
        if (this._viewModel == null)
        {
            return;
        }

        if (this.CustomAmountPanel != null)
        {
            this.CustomAmountPanel.Visibility = Visibility.Collapsed;
        }

        this._viewModel.SelectStandardPayment();
        this.UpdatePreview();
    }

    private void OnCustomChecked(object sender, RoutedEventArgs e)
    {
        if (this._viewModel == null)
        {
            return;
        }

        this.CustomAmountPanel.Visibility = Visibility.Visible;
        if (this._viewModel.SelectedLoan != null)
        {
            this.CustomAmountBox.Text = this._viewModel.SelectCustomPayment();
        }

        this.UpdatePreview();
    }

    private void OnCustomAmountTextChanged(object sender, TextChangedEventArgs e)
    {
        this.UpdatePreview();
    }

    private void OnCustomAmountLostFocus(object sender, RoutedEventArgs e)
    {
        this.UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (this._viewModel == null)
        {
            return;
        }

        if (this._viewModel.SelectedLoan == null)
        {
            this.BalanceAfterPaymentText.Text = string.Empty;
            this.RemainingTermAfterPaymentText.Text = string.Empty;
            return;
        }

        if (this.StandardRadio.IsChecked == true)
        {
            this._viewModel.SelectStandardPayment();
        }
        else
        {
            this._viewModel.UpdateCustomPayment(this.CustomAmountBox?.Text ?? string.Empty);
        }

        this.BalanceAfterPaymentText.Text = this._viewModel.PaymentPreviewBalance.ToString("C2");
        this.RemainingTermAfterPaymentText.Text = $"{this._viewModel.PaymentPreviewRemainingMonths} mo";
    }
}