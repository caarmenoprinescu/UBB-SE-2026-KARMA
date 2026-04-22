// <copyright file="CryptoTradingView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class CryptoTradingView : Page
{
    public CryptoTradingView()
    {
        this.InitializeComponent();

        var investmentRepository = new InvestmentRepository();
        var investmentService = new InvestmentService(investmentRepository);

        this.ViewModel = new CryptoTradingViewModel(investmentService);
        this.DataContext = this.ViewModel;
    }

    public CryptoTradingViewModel ViewModel { get; }

    private void OnActionTypeChecked(object sender, RoutedEventArgs e)
    {
        if (this.ViewModel == null)
        {
            return;
        }

        if (sender is RadioButton checkedRadioButton)
        {
            this.ViewModel.ActionType = checkedRadioButton.Content.ToString()?.ToUpper() ?? "BUY";
        }
    }
}