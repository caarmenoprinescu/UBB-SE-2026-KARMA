// <copyright file="InvestmentsViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;
using Microsoft.UI.Dispatching;

// Ensure this is present
public class InvestmentsViewModel : INotifyPropertyChanged
{
    private const string RefreshPricesEventName = "refreshPrices";
    private readonly DispatcherQueue? dispatcherQueue;
    private readonly InvestmentFilteringService filteringService;
    private readonly IInvestmentRepository investmentRepository;
    private readonly IMarketDataService marketDataService; // Changed to Interface
    private readonly PortfolioValuationService portfolioValuationService;
    private string activeFilterType = "All";
    private ObservableCollection<InvestmentHolding> displayedHoldings;
    private bool hasLoaded;
    private bool isPortfolioLoading;

    private Portfolio userPortfolio;

    public InvestmentsViewModel(IInvestmentRepository investmentRepository)
    {
        this.investmentRepository = investmentRepository;
        this.marketDataService = new MarketDataService();
        this.filteringService = new InvestmentFilteringService();
        this.portfolioValuationService = new PortfolioValuationService();
        this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        this.SelectFilterCommand = new RelayCommand<string>(this.ApplyFilter);

        // Corrected: RegisterPriceUpdateHandler
        this.marketDataService.RegisterPriceUpdateHandler(this.RefreshHoldingPrices);
        this.userPortfolio = new Portfolio();
        this.displayedHoldings = [];
    }

    public InvestmentsViewModel()
        : this(new InvestmentRepository())
    {
    }

    public string ActiveFilterType
    {
        get => this.activeFilterType;
        set
        {
            if (this.activeFilterType == value)
            {
                return;
            }

            this.activeFilterType = value;
            this.RefreshDisplayedHoldings();
            this.OnPropertyChanged();
        }
    }

    public ICommand SelectFilterCommand { get; }

    public bool IsEmptyStateVisible => !this.IsPortfolioLoading && this.DisplayedHoldings.Count == 0;

    public bool IsHoldingsVisible => !this.IsEmptyStateVisible;

    public ObservableCollection<InvestmentHolding> DisplayedHoldings
    {
        get => this.displayedHoldings;
        private set
        {
            this.displayedHoldings = value;
            this.OnPropertyChanged();
            this.NotifyEmptyStateChanged();
        }
    }

    public Portfolio UserPortfolio
    {
        get => this.userPortfolio;
        set
        {
            this.userPortfolio = value;
            this.OnPropertyChanged();
        }
    }

    public bool IsPortfolioLoading
    {
        get => this.isPortfolioLoading;
        set
        {
            this.isPortfolioLoading = value;
            this.OnPropertyChanged();
            this.NotifyEmptyStateChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void EnsureInitialized()
    {
        if (this.hasLoaded)
        {
            return;
        }

        this.hasLoaded = true;
        this.LoadUserPortfolio();
    }

    public void LoadUserPortfolio()
    {
        this.IsPortfolioLoading = true;

        try
        {
            this.UserPortfolio = this.investmentRepository.GetPortfolio(1);
            this.RefreshDisplayedHoldings();

            // Corrected: StartPolling
            this.marketDataService.StartPolling(this.UserPortfolio.Holdings.Select(holding => holding.Ticker).ToList());
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"LoadUserPortfolio error: {exception.Message}");
        }
        finally
        {
            this.IsPortfolioLoading = false;
        }
    }

    public void RefreshHoldingPrices()
    {
        if (this.dispatcherQueue != null && !this.dispatcherQueue.HasThreadAccess)
        {
            this.dispatcherQueue.TryEnqueue(this.RefreshHoldingPrices);
            return;
        }

        if (this.UserPortfolio?.Holdings == null || this.UserPortfolio.Holdings.Count == 0)
        {
            return;
        }

        foreach (var holding in this.UserPortfolio.Holdings)
        {
            // Corrected: GetPrice
            var updatedPrice = this.marketDataService.GetPrice(holding.Ticker);
            if (updatedPrice <= 0)
            {
                continue;
            }

            this.portfolioValuationService.UpdateHoldingValuation(holding, updatedPrice);
        }

        this.portfolioValuationService.UpdatePortfolioTotals(this.UserPortfolio);

        this.RefreshDisplayedHoldings();
        this.OnPropertyChanged(nameof(this.UserPortfolio));
        this.OnPropertyChanged(RefreshPricesEventName);
    }

    public void ApplyFilter(string? filterType)
    {
        this.ActiveFilterType = string.IsNullOrWhiteSpace(filterType) ? "All" : filterType;
    }

    private void RefreshDisplayedHoldings()
    {
        this.DisplayedHoldings.Clear();
        var holdings = this.UserPortfolio?.Holdings ?? Enumerable.Empty<InvestmentHolding>();
        var filteredHoldings = this.filteringService.FilterHoldingsByAssetType(holdings, this.ActiveFilterType);
        foreach (var holding in filteredHoldings)
        {
            this.DisplayedHoldings.Add(holding);
        }

        this.NotifyEmptyStateChanged();
    }

    public void StopMarketDataPolling()
    {
        // Corrected: StopPolling
        this.marketDataService.StopPolling();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void NotifyEmptyStateChanged()
    {
        this.OnPropertyChanged(nameof(this.IsEmptyStateVisible));
        this.OnPropertyChanged(nameof(this.IsHoldingsVisible));
    }
}