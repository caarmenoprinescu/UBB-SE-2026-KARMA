namespace KarmaBanking.App.Services
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class InvestmentService : IInvestmentService
    {
        private readonly IInvestmentRepository investmentRepository;

        // Business rules for commissions
        private const decimal CryptoTradeFeePercentage = 0.015m; // 1.5% commission
        private const decimal MinimumTradeFee = 0.50m; // Minimum commission of $0.50

        public InvestmentService(IInvestmentRepository investmentRepository)
        {
            this.investmentRepository = investmentRepository;
        }

        public async Task<bool> ExecuteCryptoTradeAsync(
            int portfolioIdentificationNumber,
            string ticker,
            string actionType,
            decimal quantity,
            decimal pricePerUnit)
        {
            // 1. Basic Input Validation
            this.ValidateTradeInputs(ticker, quantity, pricePerUnit, actionType);

            const string ActionBuy = "BUY";

            // 2. Calculate Commission
            decimal tradeValueAmount = quantity * pricePerUnit;
            decimal calculatedFee = Math.Round(tradeValueAmount * CryptoTradeFeePercentage, 2);

            if (calculatedFee < MinimumTradeFee)
            {
                calculatedFee = MinimumTradeFee;
            }

            // 3. Fetch current state to perform business logic calculations
            Portfolio portfolio = this.investmentRepository.GetPortfolio(portfolioIdentificationNumber);
            InvestmentHolding? currentHolding = portfolio.Holdings.FirstOrDefault(holding =>
                holding.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

            decimal currentQuantity = currentHolding?.Quantity ?? 0;
            decimal currentAveragePrice = currentHolding?.AveragePurchasePrice ?? 0;

            decimal finalQuantity;
            decimal finalAveragePrice;

            // 4. Perform Logic based on Action Type
            if (actionType.Equals(ActionBuy, StringComparison.OrdinalIgnoreCase))
            {
                decimal totalCostIncludingFee = tradeValueAmount + calculatedFee;

                if (portfolio.TotalValue < totalCostIncludingFee)
                {
                    throw new ArgumentException("Insufficient portfolio balance for this trade.");
                }

                // Weighted Average Price Logic
                decimal totalInvestmentCost = (currentQuantity * currentAveragePrice) + tradeValueAmount;
                finalQuantity = currentQuantity + quantity;
                finalAveragePrice = totalInvestmentCost / finalQuantity;
            }
            else
            {
                // Sell Logic Validation
                if (currentHolding == null || currentQuantity < quantity)
                {
                    throw new InvalidOperationException("Insufficient asset quantity to execute this sell order.");
                }

                finalQuantity = currentQuantity - quantity;
                finalAveragePrice = currentAveragePrice; // Purchase price remains unchanged when selling
            }

            // 5. Execution - Pass pre-calculated final values to the Repository
            try
            {
                await this.investmentRepository.RecordCryptoTradeAsync(
                    portfolioIdentificationNumber,
                    ticker,
                    actionType,
                    quantity,
                    pricePerUnit,
                    calculatedFee,
                    finalQuantity,
                    finalAveragePrice);

                return true;
            }
            catch (Exception exception)
            {
                throw new Exception($"Trade execution failed: {exception.Message}", exception);
            }
        }

        public Portfolio GetPortfolio(int userIdentificationNumber)
        {
            return this.investmentRepository.GetPortfolio(userIdentificationNumber);
        }

        public async Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(
            int portfolioIdentificationNumber,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? ticker = null)
        {
            if (portfolioIdentificationNumber <= 0)
            {
                throw new ArgumentException("Invalid portfolio identification number.", nameof(portfolioIdentificationNumber));
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                throw new ArgumentException("Start date cannot be after the end date.");
            }

            return await this.investmentRepository.GetInvestmentLogsAsync(portfolioIdentificationNumber, startDate, endDate, ticker);
        }

        private void ValidateTradeInputs(string ticker, decimal quantity, decimal price, string action)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException("Ticker symbol cannot be empty.", nameof(ticker));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }

            if (price <= 0)
            {
                throw new ArgumentException("Price per unit must be greater than zero.", nameof(price));
            }

            if (!action.Equals("BUY", StringComparison.OrdinalIgnoreCase) &&
                !action.Equals("SELL", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Action type must be either 'BUY' or 'SELL'.", nameof(action));
            }
        }
    }
}