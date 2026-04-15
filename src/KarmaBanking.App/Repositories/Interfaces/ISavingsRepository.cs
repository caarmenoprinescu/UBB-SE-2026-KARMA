using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Models.Enums;

namespace KarmaBanking.App.Repositories.Interfaces
{
    public interface ISavingsRepository
    {
        Task<SavingsAccount> CreateSavingsAccountAsync(CreateSavingsAccountDto dto, decimal apy);
        Task<List<SavingsAccount>> GetSavingsAccountsByUserIdAsync(int userId, bool includesClosedAccounts = false);
        Task<DepositResponseDto> DepositAsync(int accountId, decimal amount, string source);
        Task<ClosureResultDto> CloseSavingsAccountAsync(int accountId, int destinationAccountId, decimal transferAmount, decimal earlyClosurePenalty);
        Task<WithdrawResponseDto> WithdrawAsync(int accountId, decimal amount, string destinationLabel, decimal earlyWithdrawalPenalty);
        Task<AutoDeposit?> GetAutoDepositAsync(int accountId);
        Task SaveAutoDepositAsync(AutoDeposit autoDeposit);
        Task<List<FundingSourceOption>> GetFundingSourcesAsync(int userId);
        Task<(List<SavingsTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(int accountId, string typeFilter, int page, int pageSize);
        }
}
