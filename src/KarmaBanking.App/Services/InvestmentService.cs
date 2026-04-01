using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services
{
    internal class InvestmentService : IInvestmentService
    {
        private readonly IInvestmentRepository _investmentRepository;

        // Defined business rules for fees
        private const decimal CryptoTradeFeePercentage = 0.015m; // 1.5% fee
        private const decimal MinimumTradeFee = 0.50m; // Minimum fee of $0.50

        public InvestmentService(IInvestmentRepository investmentRepository)
        {
            _investmentRepository = investmentRepository;
        }

        public async Task<bool> ExecuteCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit)
        {
            // 1. Input Validation
            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("Ticker symbol cannot be empty.", nameof(ticker));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (pricePerUnit <= 0)
                throw new ArgumentException("Price per unit must be greater than zero.", nameof(pricePerUnit));

            if (!actionType.Equals("BUY", StringComparison.OrdinalIgnoreCase) &&
                !actionType.Equals("SELL", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Action type must be either 'BUY' or 'SELL'.", nameof(actionType));

            // 2. Fee Validation and Calculation Logic
            decimal totalTradeValue = quantity * pricePerUnit;
            decimal calculatedFee = Math.Round(totalTradeValue * CryptoTradeFeePercentage, 2);

            if (calculatedFee < MinimumTradeFee)
            {
                calculatedFee = MinimumTradeFee;
            }

            // 3. Trade Execution
            try
            {
                await _investmentRepository.RecordCryptoTradeAsync(
                    portfolioId,
                    ticker,
                    actionType,
                    quantity,
                    pricePerUnit,
                    calculatedFee);

                return true;
            }
            catch (InvalidOperationException)
            {
                // Rethrow known business logic exceptions (e.g., insufficient balance from the repository)
                throw;
            }
            catch (Exception ex)
            {
                // Capture unexpected database or execution errors
                throw new Exception($"Trade execution failed: {ex.Message}", ex);
            }
        }

        // Inside InvestmentService.cs, implement the new interface method:
        public Portfolio GetPortfolio(int userId)
        {
            // Pass the call down to the repository
            return _investmentRepository.GetPortfolio(userId);
        }

        public async Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(int portfolioId, DateTime? startDate = null, DateTime? endDate = null, string? ticker = null)
        {
            // 1. Business Logic Validation
            if (portfolioId <= 0)
            {
                throw new ArgumentException("Invalid portfolio ID.", nameof(portfolioId));
            }

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                throw new ArgumentException("Start date cannot be after the end date.");
            }

            if (ticker != null && string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException("Ticker symbol cannot be empty if provided.", nameof(ticker));
            }

            // 2. Data Retrieval
            try
            {
                return await _investmentRepository.GetInvestmentLogsAsync(portfolioId, startDate, endDate, ticker);
            }
            catch (Exception ex)
            {
                // Wrap and propagate the error so the ViewModel can display an appropriate message
                throw new Exception($"Failed to retrieve investment logs: {ex.Message}", ex);
            }
        }
    }
}
