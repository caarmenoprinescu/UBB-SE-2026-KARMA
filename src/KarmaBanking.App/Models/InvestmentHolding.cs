namespace KarmaBanking.App.Models
{
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
            get => currentPrice;
            set
            {
                currentPrice = value;
                OnPropertyChanged();
            }
        }

        public decimal UnrealizedGainLoss
        {
            get => unrealizedGainLoss;
            set
            {
                unrealizedGainLoss = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}