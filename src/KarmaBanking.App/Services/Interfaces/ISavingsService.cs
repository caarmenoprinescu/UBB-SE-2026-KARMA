using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

namespace KarmaBanking.App.Services.Interfaces
{
    public interface ISavingsService
    {
        Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId);
        Task<bool> CreateSavingsAccountAsync(SavingsAccount savingsAccount);
        Task<bool> DepositAsync(int savingsAccountId, decimal depositAmount);
        Task<bool> CloseSavingsAccountAsync(int accountId);
        Task<bool> UpdateSavingsAccountBalanceAsync(int accountId, decimal amount);
    }
}
