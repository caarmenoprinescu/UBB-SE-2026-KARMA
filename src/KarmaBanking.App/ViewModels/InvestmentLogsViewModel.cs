namespace KarmaBanking.App.ViewModels
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services.Interfaces;
    using KarmaBanking.App.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using WinRT.Interop;

    public class InvestmentLogsViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentService investmentService;

        private string? selectedTicker = "All";
        private DateTimeOffset? startDate;
        private DateTimeOffset? endDate;
        private string statusMessage = string.Empty;
        private bool isLoading;

        public InvestmentLogsViewModel(IInvestmentService investmentService)
        {
            this.investmentService = investmentService;
            this.ApplyFiltersCommand = new RelayCommand(async () => await this.LoadLogsAsync());
            this.ExportCsvCommand = new RelayCommand(async () => await this.ExportToCsvAsync());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public async Task LoadLogsAsync()
        {
            this.IsLoading = true;
            this.StatusMessage = "Loading logs...";
            this.Logs.Clear();

            try
            {
                DateTime? startDateTime = this.StartDate?.DateTime;
                DateTime? endDateTime = this.EndDate?.DateTime;
                string? tickerSymbol = this.SelectedTicker == "All" ? null : this.SelectedTicker;

                // Portfolio identification number 1 is standard for current integration
                List<InvestmentTransaction> transactionResults = await this.investmentService.GetInvestmentLogsAsync(1, startDateTime, endDateTime, tickerSymbol);

                foreach (InvestmentTransaction transactionLog in transactionResults)
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
                string csvContent = CsvExportUtility.ExportTransactionsToCsv(this.Logs);
                FileSavePicker savePicker = new FileSavePicker();

                IntPtr windowHandle = WindowNative.GetWindowHandle(App.MainAppWindow);
                InitializeWithWindow.Initialize(savePicker, windowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"InvestmentLogs_{DateTime.Now:yyyyMMdd_HHmm}";

                StorageFile destinationFile = await savePicker.PickSaveFileAsync();
                if (destinationFile != null)
                {
                    CachedFileManager.DeferUpdates(destinationFile);
                    await FileIO.WriteTextAsync(destinationFile, csvContent);
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(destinationFile);

                    this.StatusMessage = status == Windows.Storage.Provider.FileUpdateStatus.Complete
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
}