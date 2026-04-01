using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services.Interfaces;

namespace KarmaBanking.App.Services
{
    public class SavingsService : ISavingsService
    {
        private readonly ISavingsRepository savingsRepository;

        public SavingsService(ISavingsRepository savingsRepository)
        {
            this.savingsRepository = savingsRepository;
        }

        public async Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId)
        {
            return await savingsRepository.GetSavingsAccountsByUserIdAsync(userId);
        }

        public async Task<bool> CreateSavingsAccountAsync(SavingsAccount savingsAccount)
        {
            if (savingsAccount.Balance <= 0)
                return false;

            savingsAccount.CreatedAt = DateTime.Now;
            savingsAccount.AccountStatus = "Active";
            savingsAccount.AccruedInterest = 0;

            return await savingsRepository.AddSavingsAccountAsync(savingsAccount);
        }

        public async Task<bool> DepositAsync(int savingsAccountId, decimal depositAmount)
        {
            if (depositAmount <= 0)
                return false;

            return await savingsRepository.UpdateSavingsAccountBalanceAsync(savingsAccountId, depositAmount);
        }

        public async Task<bool> CloseSavingsAccountAsync(int accountId)
        {
            return await savingsRepository.CloseSavingsAccountAsync(accountId);
        }

        public async Task<bool> UpdateSavingsAccountBalanceAsync(int accountId, decimal amount)
        {
            return await savingsRepository.UpdateSavingsAccountBalanceAsync(accountId, amount);
        }

        public async Task ProcessSchedulesAsync()
        {
            var schedules = await savingsRepository.GetAllSchedulesAsync();

            foreach (var schedule in schedules)
            {
                await savingsRepository.UpdateSavingsAccountBalanceAsync(
                    schedule.AccountId,
                    schedule.Amount);
            }
        }
    }
}
