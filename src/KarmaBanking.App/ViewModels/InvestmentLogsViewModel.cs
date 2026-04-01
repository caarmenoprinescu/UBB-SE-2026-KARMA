using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers; // Added missing namespace
using WinRT.Interop;

namespace KarmaBanking.App.ViewModels
{
    public class InvestmentLogsViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentService _investmentService;

        private string? _selectedTicker = "All";
        private DateTimeOffset? _startDate;
        private DateTimeOffset? _endDate;
        private string _statusMessage = string.Empty;
        private bool _isLoading;

        public ObservableCollection<InvestmentTransaction> Logs { get; } = new();

        public RelayCommand ApplyFiltersCommand { get; }
        public RelayCommand ExportCsvCommand { get; } // New command

        public InvestmentLogsViewModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
            ApplyFiltersCommand = new RelayCommand(LoadLogsAsync);
            ExportCsvCommand = new RelayCommand(ExportToCsvAsync); // Initialize it
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
                // 1. Create the CSV string using the utility
                string csvContent = CsvExportUtility.ExportTransactionsToCsv(Logs);

                // 2. Setup the File Save Picker
                var savePicker = new FileSavePicker();

                // Get the Window Handle (HWND) for WinUI 3
                IntPtr hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("CSV File", new System.Collections.Generic.List<string>() { ".csv" });
                savePicker.SuggestedFileName = $"InvestmentLogs_{DateTime.Now:yyyyMMdd_HHmm}";

                // 3. Prompt user to pick location
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    // Prevent updates to the remote version of the file until we finish
                    CachedFileManager.DeferUpdates(file);

                    // Write the text to the file
                    await FileIO.WriteTextAsync(file, csvContent);

                    // Complete the save
                    Windows.Storage.Provider.FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);

                    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                    {
                        StatusMessage = $"Export saved successfully to {file.Name}";
                    }
                    else
                    {
                        StatusMessage = "File could not be saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
        }

        public string? SelectedTicker
        {
            get => _selectedTicker;
            set { _selectedTicker = value; OnPropertyChanged(); }
        }

        public DateTimeOffset? StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }

        public DateTimeOffset? EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Removed the duplicate 'ApplyFiltersCommand' declaration that was here

        public async Task LoadLogsAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading...";
            Logs.Clear();

            try
            {
                // Convert DateTimeOffset from the UI picker to standard DateTime
                DateTime? start = StartDate?.DateTime;
                DateTime? end = EndDate?.DateTime;

                // Map "All" back to null for the repository query
                string? ticker = SelectedTicker == "All" ? null : SelectedTicker;

                // Hardcoded portfolioId 1 for standard project flow
                var results = await _investmentService.GetInvestmentLogsAsync(1, start, end, ticker);

                foreach (var log in results)
                {
                    Logs.Add(log);
                }

                StatusMessage = Logs.Count == 0 ? "No transactions found matching the criteria." : "";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading logs: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}