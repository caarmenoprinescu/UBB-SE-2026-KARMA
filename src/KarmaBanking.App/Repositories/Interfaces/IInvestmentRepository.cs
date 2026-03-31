using KarmaBanking.App.Models;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface IInvestmentRepository
    {
        Portfolio GetPortfolio(int userId);
    }
}
