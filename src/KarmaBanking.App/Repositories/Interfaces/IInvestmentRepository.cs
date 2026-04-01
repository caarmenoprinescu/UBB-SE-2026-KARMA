using KarmaBanking.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface IInvestmentRepository
    {
        Portfolio GetPortfolio(int userId);
        Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees);

        // New method for BA-60
        Task<List<InvestmentTransaction>> GetInvestmentLogsAsync(int portfolioId, DateTime? startDate = null, DateTime? endDate = null, string? ticker = null);
    }
}