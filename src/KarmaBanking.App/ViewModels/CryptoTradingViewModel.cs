using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
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

        // New properties for UI Synchronization (BA-58)
        private decimal _currentBalance;
        private decimal _estimatedFee;
        private decimal _totalAmount;

        public CryptoTradingViewModel(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
            SubmitTradeCommand = new RelayCommand(ExecuteTradeAsync, CanExecuteTrade);

            // Load the initial wallet balance when the ViewModel is created
            LoadWalletBalance();
        }

        public string SelectedTicker
        {
            get => _selectedTicker;
            set
            {
                _selectedTicker = value;
                OnPropertyChanged();
                // Recalculate when the asset changes (since price changes)
                UpdateCalculations();
                SubmitTradeCommand.RaiseCanExecuteChanged();
            }
        }

        public string ActionType
        {
            get => _actionType;
            set
            {
                _actionType = value;
                OnPropertyChanged();
                // Recalculate fees and totals when switching between Buy/Sell
                UpdateCalculations();
            }
        }

        public string QuantityText
        {
            get => _quantityText;
            set
            {
                _quantityText = value;
                OnPropertyChanged();
                // Live sync: Recalculate whenever the user types a new quantity
                UpdateCalculations();
                SubmitTradeCommand.RaiseCanExecuteChanged();
            }
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

        // --- Synchronized Properties ---

        public decimal CurrentBalance
        {
            get => _currentBalance;
            set { _currentBalance = value; OnPropertyChanged(); }
        }

        public decimal EstimatedFee
        {
            get => _estimatedFee;
            set { _estimatedFee = value; OnPropertyChanged(); }
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        public RelayCommand SubmitTradeCommand { get; }

        private void LoadWalletBalance()
        {
            try
            {
                // Hardcoded userId 1 for standard project flow. 
                // Fetches the portfolio to display the available funds.
                Portfolio portfolio = _investmentService.GetPortfolio(1);
                if (portfolio != null)
                {
                    CurrentBalance = portfolio.TotalValue;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to sync wallet balance.";
            }
        }

        private void UpdateCalculations()
        {
            // Reset values if input is empty or invalid
            if (string.IsNullOrWhiteSpace(QuantityText) || !decimal.TryParse(QuantityText, out decimal quantity) || quantity <= 0)
            {
                EstimatedFee = 0;
                TotalAmount = 0;
                return;
            }

            // Mock price selection (in a real scenario, this fetches a live quote)
            decimal currentPrice = SelectedTicker == "BTC" ? 65000m : 3000m;
            decimal tradeValue = quantity * currentPrice;

            // Apply the fee logic defined in BA-56 (1.5% fee, minimum $0.50)
            decimal calculatedFee = Math.Round(tradeValue * 0.015m, 2);
            EstimatedFee = calculatedFee < 0.50m ? 0.50m : calculatedFee;

            // Sync total cost: BUY means cost + fee. SELL means revenue - fee.
            if (ActionType == "BUY")
            {
                TotalAmount = tradeValue + EstimatedFee;
            }
            else // SELL
            {
                TotalAmount = tradeValue - EstimatedFee;
            }
        }

        private bool CanExecuteTrade()
        {
            if (_isSubmitting || string.IsNullOrWhiteSpace(QuantityText) || !decimal.TryParse(QuantityText, out decimal qty) || qty <= 0)
                return false;

            // Optional: Prevent BUY if TotalAmount exceeds CurrentBalance
            if (ActionType == "BUY" && TotalAmount > CurrentBalance)
                return false;

            return true;
        }

        private async Task ExecuteTradeAsync()
        {
            if (!decimal.TryParse(QuantityText, out decimal quantity)) return;

            IsSubmitting = true;
            StatusMessage = "Executing trade...";

            try
            {
                decimal mockPrice = SelectedTicker == "BTC" ? 65000m : 3000m;

                await _investmentService.ExecuteCryptoTradeAsync(1, SelectedTicker, ActionType, quantity, mockPrice);

                StatusMessage = $"Successfully executed {ActionType} for {quantity} {SelectedTicker}.";
                QuantityText = string.Empty;

                // Re-sync the wallet balance after a successful trade
                LoadWalletBalance();
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