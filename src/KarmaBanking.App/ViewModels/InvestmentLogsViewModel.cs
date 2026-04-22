// <copyright file="InvestmentLogsViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;
using WinRT.Interop;

public class InvestmentLogsViewModel : INotifyPropertyChanged
{
    private readonly IInvestmentService investmentService;
    private DateTimeOffset? endDate;
    private bool isLoading;

    private string? selectedTicker = "All";
    private DateTimeOffset? startDate;
    private string statusMessage = string.Empty;

    public InvestmentLogsViewModel(IInvestmentService investmentService)
    {
        this.investmentService = investmentService;
        this.ApplyFiltersCommand = new RelayCommand(async () => await this.LoadLogsAsync());
        this.ExportCsvCommand = new RelayCommand(async () => await this.ExportToCsvAsync());
    }

    public ObservableCollection<InvestmentTransaction> Logs { get; } = new();

    public RelayCommand ApplyFiltersCommand { get; }

    public RelayCommand ExportCsvCommand { get; }

    public string? SelectedTicker
    {
        get => this.selectedTicker;
        set
        {
            this.selectedTicker = value;
            this.OnPropertyChanged();
        }
    }

    public DateTimeOffset? StartDate
    {
        get => this.startDate;
        set
        {
            this.startDate = value;
            this.OnPropertyChanged();
        }
    }

    public DateTimeOffset? EndDate
    {
        get => this.endDate;
        set
        {
            this.endDate = value;
            this.OnPropertyChanged();
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

    public bool IsLoading
    {
        get => this.isLoading;
        set
        {
            this.isLoading = value;
            this.OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadLogsAsync()
    {
        this.IsLoading = true;
        this.StatusMessage = "Loading logs...";
        this.Logs.Clear();

        try
        {
            var startDateTime = this.StartDate?.DateTime;
            var endDateTime = this.EndDate?.DateTime;
            var tickerSymbol = this.SelectedTicker == "All" ? null : this.SelectedTicker;

            // Portfolio identification number 1 is standard for current integration
            var transactionResults = await this.investmentService.GetInvestmentLogsAsync(
                1,
                startDateTime,
                endDateTime,
                tickerSymbol);

            foreach (var transactionLog in transactionResults)
            {
                this.Logs.Add(transactionLog);
            }

            this.StatusMessage = this.Logs.Count == 0 ? "No transactions found matching the criteria." : string.Empty;
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Error loading logs: {exception.Message}";
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    private async Task ExportToCsvAsync()
    {
        if (this.Logs.Count == 0)
        {
            this.StatusMessage = "No data to export.";
            return;
        }

        try
        {
            var csvContent = CsvExportUtility.ExportTransactionsToCsv(this.Logs);
            var savePicker = new FileSavePicker();

            var windowHandle = WindowNative.GetWindowHandle(App.MainAppWindow);
            InitializeWithWindow.Initialize(savePicker, windowHandle);

            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });
            savePicker.SuggestedFileName = $"InvestmentLogs_{DateTime.Now:yyyyMMdd_HHmm}";

            var destinationFile = await savePicker.PickSaveFileAsync();
            if (destinationFile != null)
            {
                CachedFileManager.DeferUpdates(destinationFile);
                await FileIO.WriteTextAsync(destinationFile, csvContent);
                var status = await CachedFileManager.CompleteUpdatesAsync(destinationFile);

                this.StatusMessage = status == FileUpdateStatus.Complete
                    ? $"Export saved successfully to {destinationFile.Name}"
                    : "File could not be saved.";
            }
        }
        catch (Exception exception)
        {
            this.StatusMessage = $"Export failed: {exception.Message}";
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}