using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface ISavingsRepository
    {
        Task<bool> AddSavingsAccountAsync(SavingsAccount savingsAccount);
        Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId);
        Task<bool> UpdateSavingsAccountBalanceAsync(int savingsAccountId, decimal amountToAdd);
        Task<bool> CloseSavingsAccountAsync(int savingsAccountId);
        Task<List<(int AccountId, decimal Amount)>> GetAllSchedulesAsync();
        Task<bool> CreateScheduleAsync(int savingsAccountId, decimal amount, string frequency);
    }
}