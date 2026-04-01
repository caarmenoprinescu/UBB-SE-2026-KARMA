using KarmaBanking.App.Models;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface IInvestmentRepository
    {
        Portfolio GetPortfolio(int userId);
        Task RecordCryptoTradeAsync(int portfolioId, string ticker, string actionType, decimal quantity, decimal pricePerUnit, decimal fees);
    }
}
