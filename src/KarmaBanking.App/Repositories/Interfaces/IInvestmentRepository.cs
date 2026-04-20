namespace KarmaBanking.App.Repositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using KarmaBanking.App.Models;

    public interface IInvestmentRepository
    {
        Portfolio GetPortfolio(int userIdentificationNumber);

        Task RecordCryptoTradeAsync(
            int portfolioIdentificationNumber,
            string ticker,
            string actionType,
            decimal quantity,
            decimal pricePerUnit,
            decimal fees);

        Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(
            int portfolioIdentificationNumber,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? ticker = null);
    }
}