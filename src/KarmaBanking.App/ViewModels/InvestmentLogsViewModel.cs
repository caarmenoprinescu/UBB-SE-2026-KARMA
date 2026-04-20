namespace KarmaBanking.App.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services.Interfaces;
    using KarmaBanking.App.Utils;
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
            ApplyFiltersCommand = new RelayCommand(async () => await LoadLogsAsync());
            ExportCsvCommand = new RelayCommand(async () => await ExportToCsvAsync());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<InvestmentTransaction> Logs { get; } = new ObservableCollection<InvestmentTransaction>();

        public RelayCommand ApplyFiltersCommand { get; }

        public RelayCommand ExportCsvCommand { get; }

        public string? SelectedTicker
        {
            get => selectedTicker;
            set
            {
                selectedTicker = value;
                OnPropertyChanged();
            }
        }

        public DateTimeOffset? StartDate
        {
            get => startDate;
            set
            {
                startDate = value;
                OnPropertyChanged();
            }
        }

        public DateTimeOffset? EndDate
        {
            get => endDate;
            set
            {
                endDate = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                statusMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => isLoading;
            set
            {
                isLoading = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadLogsAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading logs...";
            Logs.Clear();

            try
            {
                DateTime? startDateTime = StartDate?.DateTime;
                DateTime? endDateTime = EndDate?.DateTime;
                string? tickerSymbol = SelectedTicker == "All" ? null : SelectedTicker;

                // Portfolio identification number 1 is standard for current integration
                List<InvestmentTransaction> transactionResults = await investmentService.GetInvestmentLogsAsync(1, startDateTime, endDateTime, tickerSymbol);

                foreach (InvestmentTransaction transactionLog in transactionResults)
                {
                    Logs.Add(transactionLog);
                }

                StatusMessage = Logs.Count == 0 ? "No transactions found matching the criteria." : string.Empty;
            }
            catch (Exception exception)
            {
                StatusMessage = $"Error loading logs: {exception.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportToCsvAsync()
        {
            if (Logs.Count == 0)
            {
                StatusMessage = "No data to export.";
                return;
            }

            try
            {
                string csvContent = CsvExportUtility.ExportTransactionsToCsv(Logs);
                FileSavePicker savePicker = new FileSavePicker();

                IntPtr windowHandle = WindowNative.GetWindowHandle(App.MainAppWindow);
                InitializeWithWindow.Initialize(savePicker, windowHandle);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV File", new List<string> { ".csv" });
                savePicker.SuggestedFileName = $"InvestmentLogs_{DateTime.Now:yyyyMMdd_HHmm}";

                StorageFile destinationFile = await savePicker.PickSaveFileAsync();
                if (destinationFile != null)
                {
                    CachedFileManager.DeferUpdates(destinationFile);
                    await FileIO.WriteTextAsync(destinationFile, csvContent);
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(destinationFile);

                    StatusMessage = status == Windows.Storage.Provider.FileUpdateStatus.Complete
                        ? $"Export saved successfully to {destinationFile.Name}"
                        : "File could not be saved.";
                }
            }
            catch (Exception exception)
            {
                StatusMessage = $"Export failed: {exception.Message}";
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}