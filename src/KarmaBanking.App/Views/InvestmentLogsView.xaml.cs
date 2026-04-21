// <copyright file="InvestmentLogsView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

public sealed partial class InvestmentLogsView : Page
{
    public InvestmentLogsView()
    {
        this.InitializeComponent();

        var repository = new InvestmentRepository();
        var service = new InvestmentService(repository);

        this.ViewModel = new InvestmentLogsViewModel(service);
        this.DataContext = this.ViewModel;

        // Load logs initially with no filters applied
        _ = this.ViewModel.LoadLogsAsync();
    }

    public InvestmentLogsViewModel ViewModel { get; }
}