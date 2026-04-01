using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;

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

        public InvestmentLogsViewModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
            ApplyFiltersCommand = new RelayCommand(LoadLogsAsync);
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

        public RelayCommand ApplyFiltersCommand { get; }

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