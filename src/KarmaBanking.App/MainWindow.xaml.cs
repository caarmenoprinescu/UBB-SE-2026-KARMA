// <copyright file="MainWindow.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App;

using KarmaBanking.App.Views;
using Microsoft.UI.Xaml;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Activated += this.OnFirstActivated;
    }

    private void OnFirstActivated(object sender, WindowActivatedEventArgs args)
    {
        this.Activated -= this.OnFirstActivated;

        // this.MainFrame.Navigate(typeof(LoansView));
        this.MainFrame.Navigate(typeof(CryptoTradingView));
    }
}