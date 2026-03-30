using System.Threading.Tasks;
using KarmaBanking.App.Models;

namespace KarmaBanking.App.Services.Interfaces
{
    public interface ISavingsService
    {
        Task<bool> CreateSavingsAccountAsync(SavingsAccount savingsAccount);
        Task<bool> DepositAsync(int savingsAccountId, decimal depositAmount);
    }
}
