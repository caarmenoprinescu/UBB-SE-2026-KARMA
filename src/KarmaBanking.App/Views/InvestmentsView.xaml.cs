// <copyright file="InvestmentsView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using KarmaBanking.App.Repositories;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class InvestmentsView : Page
{
    public InvestmentsView()
    {
        this.InitializeComponent();

        this.ViewModel = new InvestmentsViewModel(new InvestmentRepository());
        this.DataContext = this.ViewModel;

        this.Loaded += this.OnPageLoaded;
        this.Unloaded += this.OnPageUnloaded;
    }

    public InvestmentsViewModel ViewModel { get; }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        this.ViewModel.EnsureInitialized();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        this.ViewModel.StopMarketDataPolling();
        this.Loaded -= this.OnPageLoaded;
        this.Unloaded -= this.OnPageUnloaded;
    }
}