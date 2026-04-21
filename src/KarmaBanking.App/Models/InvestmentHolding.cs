// <copyright file="InvestmentHolding.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class InvestmentHolding : INotifyPropertyChanged
{
    private decimal currentPrice;
    private decimal unrealizedGainLoss;

    public int IdentificationNumber { get; set; }

    public int PortfolioIdentificationNumber { get; set; }

    public string Ticker { get; set; } = string.Empty;

    public string AssetType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal AveragePurchasePrice { get; set; }

    public decimal CurrentPrice
    {
        get => this.currentPrice;
        set
        {
            this.currentPrice = value;
            this.OnPropertyChanged();
        }
    }

    public decimal UnrealizedGainLoss
    {
        get => this.unrealizedGainLoss;
        set
        {
            this.unrealizedGainLoss = value;
            this.OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}