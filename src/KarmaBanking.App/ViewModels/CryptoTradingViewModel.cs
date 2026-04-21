// <copyright file="CryptoTradingViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Services;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;

public class CryptoTradingViewModel : INotifyPropertyChanged
{
    private readonly IInvestmentService investmentService;
    private readonly CryptoTradeCalculationService tradeCalculationService;

    private decimal currentWalletBalance;
    private decimal estimatedTransactionFee;
    private bool isSubmitting;
    private string quantityText = string.Empty;
    private string selectedActionType = "BUY";

    private string selectedTicker = "BTC";
    private string statusMessage = string.Empty;
    private decimal totalTransactionAmount;

    public CryptoTradingViewModel(IInvestmentService investmentService)
    {
        this.investmentService = investmentService;
        this.tradeCalculationService = new CryptoTradeCalculationService();
        this.SubmitTradeCommand = new RelayCommand(async () => await this.ExecuteTradeAsync(), this.CanExecuteTrade);
        this.LoadWalletBalance();
    }

    public RelayCommand SubmitTradeCommand { get; }

    public string SelectedTicker
    {
        get => this.selectedTicker;
        set
        {
            this.selectedTicker = value;
            this.OnPropertyChanged();
            this.UpdateCalculations();
            this.SubmitTradeCommand.RaiseCanExecuteChanged();
        }
    }

    public string ActionType
    {
        get => this.selectedActionType;
        set
        {
            this.selectedActionType = value;
            this.OnPropertyChanged();
            this.UpdateCalculations();
        }
    }

    public string QuantityText
    {
        get => this.quantityText;
        set
        {
            this.quantityText = value;
            this.OnPropertyChanged();
            this.UpdateCalculations();
            this.SubmitTradeCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        set
        {
            this.statusMessage = value;
            this.OnPropertyChanged();
        }
    }

    public bool IsSubmitting
    {
        get => this.isSubmitting;
        set
        {
            this.isSubmitting = value;
            this.OnPropertyChanged();
            this.SubmitTradeCommand.RaiseCanExecuteChanged();
        }
    }

    public decimal CurrentBalance
    {
        get => this.currentWalletBalance;
        set
        {
            this.currentWalletBalance = value;
            this.OnPropertyChanged();
        }
    }

    public decimal EstimatedFee
    {
        get => this.estimatedTransactionFee;
        set
        {
            this.estimatedTransactionFee = value;
            this.OnPropertyChanged();
        }
    }

    public decimal TotalAmount
    {
        get => this.totalTransactionAmount;
        set
        {
            this.totalTransactionAmount = value;
            this.OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void LoadWalletBalance()
    {
        try
        {
            // Folosim identificatorul hardcodat 1 pentru flow-ul actual al proiectului
            var userPortfolio = this.investmentService.GetPortfolio(1);
            if (userPortfolio != null)
            {
                this.CurrentBalance = userPortfolio.TotalValue;
            }
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Failed to sync wallet balance: {exception.Message}";
        }
    }

    private void UpdateCalculations()
    {
        if (!this.tradeCalculationService.TryParsePositiveQuantity(this.QuantityText, out var quantity))
        {
            this.EstimatedFee = 0;
            this.TotalAmount = 0;
            return;
        }

        var (estimatedFee, totalAmount) =
            this.tradeCalculationService.CalculateTradePreview(this.SelectedTicker, this.ActionType, quantity);
        this.EstimatedFee = estimatedFee;
        this.TotalAmount = totalAmount;
    }

    private bool CanExecuteTrade()
    {
        return this.tradeCalculationService.CanExecuteTrade(
            this.IsSubmitting,
            this.QuantityText,
            this.ActionType,
            this.TotalAmount,
            this.CurrentBalance);
    }

    private async Task ExecuteTradeAsync()
    {
        if (!this.tradeCalculationService.TryParsePositiveQuantity(this.QuantityText, out var quantity))
        {
            return;
        }

        this.IsSubmitting = true;
        this.StatusMessage = "Executing trade...";

        try
        {
            var mockPrice = this.tradeCalculationService.GetMockMarketPrice(this.SelectedTicker);

            await this.investmentService.ExecuteCryptoTradeAsync(
                1,
                this.SelectedTicker,
                this.ActionType,
                quantity,
                mockPrice);

            this.StatusMessage = $"Successfully executed {this.ActionType} for {quantity} {this.SelectedTicker}.";
            this.QuantityText = string.Empty;

            this.LoadWalletBalance();
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Error: {exception.Message}";
        }
        finally
        {
            this.IsSubmitting = false;
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}