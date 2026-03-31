using System;

namespace KarmaBanking.App.Models
{
    public class InvestmentHolding
    {
        public int Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvgPurchasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal UnrealizedGainLoss { get; set; }
    }
}
