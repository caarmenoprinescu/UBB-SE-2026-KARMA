using System;

namespace KarmaBanking.App.Services
{
    public class CryptoTradeCalculationService
    {
        private const decimal FeeRate = 0.015m;
        private const decimal MinimumFee = 0.50m;

        public bool TryParsePositiveQuantity(string quantityText, out decimal quantity)
        {
            if (decimal.TryParse(quantityText, out quantity) && quantity > 0)
            {
                return true;
            }

            quantity = 0m;
            return false;
        }

        public decimal GetMockMarketPrice(string ticker)
            => ticker == "BTC" ? 65000m : 3000m;

        public (decimal EstimatedFee, decimal TotalAmount) CalculateTradePreview(string ticker, string actionType, decimal quantity)
        {
            decimal tradeValue = quantity * GetMockMarketPrice(ticker);
            decimal calculatedFee = Math.Round(tradeValue * FeeRate, 2);
            decimal estimatedFee = calculatedFee < MinimumFee ? MinimumFee : calculatedFee;
            decimal totalAmount = actionType == "BUY"
                ? tradeValue + estimatedFee
                : tradeValue - estimatedFee;

            return (estimatedFee, totalAmount);
        }

        public bool CanExecuteTrade(bool isSubmitting, string quantityText, string actionType, decimal totalAmount, decimal currentBalance)
        {
            if (isSubmitting || !TryParsePositiveQuantity(quantityText, out _))
            {
                return false;
            }

            if (actionType == "BUY" && totalAmount > currentBalance)
            {
                return false;
            }

            return true;
        }
    }
}
