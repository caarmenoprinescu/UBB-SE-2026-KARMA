using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;

namespace KarmaBanking.App.ViewModels
{
    public class CryptoTradingViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentService _investmentService;

        private string _selectedTicker = "BTC";
        private string _actionType = "BUY";
        private string _quantityText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _isSubmitting;

        public CryptoTradingViewModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
            SubmitTradeCommand = new RelayCommand(ExecuteTradeAsync, CanExecuteTrade);
        }

        public string SelectedTicker
        {
            get => _selectedTicker;
            set { _selectedTicker = value; OnPropertyChanged(); SubmitTradeCommand.RaiseCanExecuteChanged(); }
        }

        public string ActionType
        {
            get => _actionType;
            set { _actionType = value; OnPropertyChanged(); }
        }

        public string QuantityText
        {
            get => _quantityText;
            set { _quantityText = value; OnPropertyChanged(); SubmitTradeCommand.RaiseCanExecuteChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsSubmitting
        {
            get => _isSubmitting;
            set { _isSubmitting = value; OnPropertyChanged(); SubmitTradeCommand.RaiseCanExecuteChanged(); }
        }

        public RelayCommand SubmitTradeCommand { get; }

        private bool CanExecuteTrade()
        {
            return !_isSubmitting && !string.IsNullOrWhiteSpace(QuantityText) && decimal.TryParse(QuantityText, out decimal qty) && qty > 0;
        }

        private async Task ExecuteTradeAsync()
        {
            if (!decimal.TryParse(QuantityText, out decimal quantity)) return;

            IsSubmitting = true;
            StatusMessage = "Executing trade...";

            try
            {
                // Hardcoding PortfolioId (1) and a mock price for demonstration.
                decimal mockPrice = SelectedTicker == "BTC" ? 65000m : 3000m;

                await _investmentService.ExecuteCryptoTradeAsync(1, SelectedTicker, ActionType, quantity, mockPrice);
                StatusMessage = $"Successfully executed {ActionType} for {quantity} {SelectedTicker}.";
                QuantityText = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}