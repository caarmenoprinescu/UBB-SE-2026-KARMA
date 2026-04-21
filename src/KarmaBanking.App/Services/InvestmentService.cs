namespace KarmaBanking.App.Services
{
    using KarmaBanking.App.Models;
    using KarmaBanking.App.Repositories.Interfaces;
    using KarmaBanking.App.Services.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class InvestmentService : IInvestmentService
    {
        private readonly IInvestmentRepository investmentRepository;

        // Reguli de business pentru comisioane
        private const decimal CryptoTradeFeePercentage = 0.015m; // 1.5% comision
        private const decimal MinimumTradeFee = 0.50m; // Comision minim de $0.50

        public InvestmentService(IInvestmentRepository investmentRepository)
        {
            this.investmentRepository = investmentRepository;
        }

        public async Task<bool> ExecuteCryptoTradeAsync(int portfolioIdentificationNumber, string ticker, string actionType, decimal quantity, decimal pricePerUnit)
        {
            // 1. Validarea datelor de intrare
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException("Ticker symbol cannot be empty.", nameof(ticker));
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
            }

            if (pricePerUnit <= 0)
            {
                throw new ArgumentException("Price per unit must be greater than zero.", nameof(pricePerUnit));
            }

            const string ActionBuy = "BUY";
            const string ActionSell = "SELL";

            if (!actionType.Equals(ActionBuy, StringComparison.OrdinalIgnoreCase) &&
                !actionType.Equals(ActionSell, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Action type must be either 'BUY' or 'SELL'.", nameof(actionType));
            }

            // 2. Calculul comisionului
            decimal totalTradeValue = quantity * pricePerUnit;
            decimal calculatedFee = Math.Round(totalTradeValue * CryptoTradeFeePercentage, 2);

            if (calculatedFee < MinimumTradeFee)
            {
                calculatedFee = MinimumTradeFee;
            }

            if (actionType.Equals(ActionBuy, StringComparison.OrdinalIgnoreCase))
            {
                Portfolio portfolio = investmentRepository.GetPortfolio(portfolioIdentificationNumber);
                decimal totalCost = totalTradeValue + calculatedFee;

                if (portfolio.TotalValue < totalCost)
                {
                    throw new ArgumentException("Insufficient portfolio balance for this trade.");
                }
            }

            // 3. Execuția tranzacției
            try
            {
                await investmentRepository.RecordCryptoTradeAsync(
                    portfolioIdentificationNumber,
                    ticker,
                    actionType,
                    quantity,
                    pricePerUnit,
                    calculatedFee);

                return true;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new Exception($"Trade execution failed: {exception.Message}", exception);
            }
        }

        public Portfolio GetPortfolio(int userIdentificationNumber)
        {
            return investmentRepository.GetPortfolio(userIdentificationNumber);
        }

        public async Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(int portfolioIdentificationNumber, DateTime? startDate = null, DateTime? endDate = null, string? ticker = null)
        {
            if (portfolioIdentificationNumber <= 0)
            {
                throw new ArgumentException("Invalid portfolio identification number.", nameof(portfolioIdentificationNumber));
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                throw new ArgumentException("Start date cannot be after the end date.");
            }

            try
            {
                return await investmentRepository.GetInvestmentLogsAsync(portfolioIdentificationNumber, startDate, endDate, ticker);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to retrieve investment logs: {exception.Message}", exception);
            }
        }
    }
}