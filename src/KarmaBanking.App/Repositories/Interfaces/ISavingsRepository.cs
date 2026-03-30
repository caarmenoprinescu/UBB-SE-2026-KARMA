using System.Threading.Tasks;
using KarmaBanking.App.Models;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface ISavingsRepository
    {
        Task<bool> AddSavingsAccountAsync(SavingsAccount savingsAccount);
        Task<bool> UpdateSavingsAccountBalanceAsync(int savingsAccountId, decimal amountToAdd);

        Task<bool> CloseSavingsAccountAsync(int savingsAccountId);
    }
}