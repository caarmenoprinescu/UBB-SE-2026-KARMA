namespace KarmaBanking.App.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Services.Interfaces;
    using KarmaBanking.App.Utils;

    public class CryptoTradingViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentService investmentService;

        private string selectedTicker = "BTC";
        private string selectedActionType = "BUY";
        private string quantityText = string.Empty;
        private string statusMessage = string.Empty;
        private bool isSubmitting;

        private decimal currentWalletBalance;
        private decimal estimatedTransactionFee;
        private decimal totalTransactionAmount;

        public CryptoTradingViewModel(IInvestmentService investmentService)
        {
            this.investmentService = investmentService;
            SubmitTradeCommand = new RelayCommand(async () => await ExecuteTradeAsync(), CanExecuteTrade);
            LoadWalletBalance();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public RelayCommand SubmitTradeCommand { get; }

        public string SelectedTicker
        {
            get => selectedTicker;
            set
            {
                selectedTicker = value;
                OnPropertyChanged();
                UpdateCalculations();
                SubmitTradeCommand.RaiseCanExecuteChanged();
            }
        }

        public string ActionType
        {
            get => selectedActionType;
            set
            {
                selectedActionType = value;
                OnPropertyChanged();
                UpdateCalculations();
            }
        }

        public string QuantityText
        {
            get => quantityText;
            set
            {
                quantityText = value;
                OnPropertyChanged();
                UpdateCalculations();
                SubmitTradeCommand.RaiseCanExecuteChanged();
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

        public bool IsSubmitting
        {
            get => isSubmitting;
            set
            {
                isSubmitting = value;
                OnPropertyChanged();
                SubmitTradeCommand.RaiseCanExecuteChanged();
            }
        }

        public decimal CurrentBalance
        {
            get => currentWalletBalance;
            set
            {
                currentWalletBalance = value;
                OnPropertyChanged();
            }
        }

        public decimal EstimatedFee
        {
            get => estimatedTransactionFee;
            set
            {
                estimatedTransactionFee = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalAmount
        {
            get => totalTransactionAmount;
            set
            {
                totalTransactionAmount = value;
                OnPropertyChanged();
            }
        }

        private void LoadWalletBalance()
        {
            try
            {
                // Folosim identificatorul hardcodat 1 pentru flow-ul actual al proiectului
                Portfolio userPortfolio = investmentService.GetPortfolio(1);
                if (userPortfolio != null)
                {
                    CurrentBalance = userPortfolio.TotalValue;
                }
            }
            catch (Exception exception)
            {
                StatusMessage = $"Failed to sync wallet balance: {exception.Message}";
            }
        }

        private void UpdateCalculations()
        {
            if (string.IsNullOrWhiteSpace(QuantityText) || !decimal.TryParse(QuantityText, out decimal quantity) || quantity <= 0)
            {
                EstimatedFee = 0;
                TotalAmount = 0;
                return;
            }

            // Simulam pretul curent (intr-un scenariu real, acesta vine dintr-un serviciu de Market Data)
            decimal currentMarketPrice = SelectedTicker == "BTC" ? 65000m : 3000m;
            decimal tradeValue = quantity * currentMarketPrice;

            // Logica de calcul a comisionului (1.5% cu minim 0.50$)
            decimal calculatedFee = Math.Round(tradeValue * 0.015m, 2);
            EstimatedFee = calculatedFee < 0.50m ? 0.50m : calculatedFee;

            if (ActionType == "BUY")
            {
                TotalAmount = tradeValue + EstimatedFee;
            }
            else
            {
                TotalAmount = tradeValue - EstimatedFee;
            }
        }

        private bool CanExecuteTrade()
        {
            bool hasValidQuantity = decimal.TryParse(QuantityText, out decimal quantity) && quantity > 0;

            if (IsSubmitting || !hasValidQuantity)
            {
                return false;
            }

            // Validare de baza pentru fonduri insuficiente la cumparare
            if (ActionType == "BUY" && TotalAmount > CurrentBalance)
            {
                return false;
            }

            return true;
        }

        private async Task ExecuteTradeAsync()
        {
            if (!decimal.TryParse(QuantityText, out decimal quantity))
            {
                return;
            }

            IsSubmitting = true;
            StatusMessage = "Executing trade...";

            try
            {
                decimal mockPrice = SelectedTicker == "BTC" ? 65000m : 3000m;

                await investmentService.ExecuteCryptoTradeAsync(1, SelectedTicker, ActionType, quantity, mockPrice);

                StatusMessage = $"Successfully executed {ActionType} for {quantity} {SelectedTicker}.";
                QuantityText = string.Empty;

                LoadWalletBalance();
            }
            catch (Exception exception)
            {
                StatusMessage = $"Error: {exception.Message}";
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}