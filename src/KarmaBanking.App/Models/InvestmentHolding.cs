using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KarmaBanking.App.Models
{
    public class InvestmentHolding : INotifyPropertyChanged
    {
        private decimal _currentPrice;
        private decimal _unrealizedGainLoss;

        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvgPurchasePrice { get; set; }

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                _currentPrice = value;
                OnPropertyChanged();
            }
        }

        public decimal UnrealizedGainLoss
        {
            get => _unrealizedGainLoss;
            set
            {
                _unrealizedGainLoss = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
