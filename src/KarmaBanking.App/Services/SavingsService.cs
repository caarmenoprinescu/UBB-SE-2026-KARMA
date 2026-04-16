using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Repositories.Interfaces;
using KarmaBanking.App.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KarmaBanking.App.Services
{
    public class SavingsService : ISavingsService
    {
        private readonly ISavingsRepository savingsRepository;

        private const int MAX_ACTIVE_ACCOUNTS = 5;

        private const decimal FIXED_DEPOSIT_APY = 0.04m;
        private const decimal GOAL_SAVINGS_APY = 0.03m;
        private const decimal HIGH_YIELD_APY = 0.03m;
        private const decimal DEFAULT_APY = 0.02m;

        private const decimal DECIMAL_EARLY_CLOSURE_PENALTY = 0.02m;
        private const decimal DECIMAL_EARLY_WITHDRAWAL_PENALTY = 0.02m;


        public SavingsService(ISavingsRepository savingsRepository)
        {
            this.savingsRepository = savingsRepository;
        }


        public async Task<SavingsAccount> CreateAccountAsync(CreateSavingsAccountDto dto)
        {
            // BA-9: max 5 active accounts per user
            var activeAccountsList = await savingsRepository.GetSavingsAccountsByUserIdAsync(dto.UserId, includesClosedAccounts: false);
            if (activeAccountsList.Count >= MAX_ACTIVE_ACCOUNTS)
                throw new InvalidOperationException("You cannot have more than 5 active savings accounts.");

            // BA-9: GoalSavings requires a future target date
            if (dto.SavingsType == "GoalSavings")
            {
                if (!dto.TargetDate.HasValue)
                    throw new ArgumentException("GoalSavings accounts require a target date.");

                if (dto.TargetDate.Value <= DateTime.Today)
                    throw new ArgumentException("Target date must be in the future.");

                if (!dto.TargetAmount.HasValue || dto.TargetAmount.Value <= 0)
                    throw new ArgumentException("GoalSavings accounts require a positive target amount.");
            }

            decimal apy = dto.SavingsType switch
            {
                "FixedDeposit" => FIXED_DEPOSIT_APY,
                "GoalSavings" => GOAL_SAVINGS_APY,
                "HighYield" => HIGH_YIELD_APY,
                _ => DEFAULT_APY
            };

            return await savingsRepository.CreateSavingsAccountAsync(dto, apy);
        }


        // this method smells a bit...
        public Task<List<SavingsAccount>> GetAccountsAsync(int userId, bool includesClosed = false)
        {
            return savingsRepository.GetSavingsAccountsByUserIdAsync(userId, includesClosed);
        }


        public async Task<DepositResponseDto> DepositAsync(int accountId, decimal amount, string source, int userId)
        {
            if (amount <= 0)
                throw new ArgumentException("Deposit amount must be positive.");

            // BA-14: get the account and validate ownership + status
            var userAccountsList = await savingsRepository.GetSavingsAccountsByUserIdAsync(userId, includesClosedAccounts: true);
            var destinationAccount = userAccountsList.Find(account => account.Id == accountId)
                ?? throw new InvalidOperationException("Account not found or does not belong to you.");

            // BA-14: 422 for closed or locked
            if (destinationAccount.AccountStatus == "Closed")
                throw new InvalidOperationException("Cannot deposit into a closed account.");

            if (destinationAccount.DisplayStatus == "Matured")
                throw new InvalidOperationException("Cannot deposit into a matured account.");

            return await savingsRepository.DepositAsync(accountId, amount, source);
        }


        public async Task<ClosureResultDto> CloseAccountAsync(int accountId, int destinationAccountId, int userId)
        {
            var userAccountsList = await savingsRepository.GetSavingsAccountsByUserIdAsync(userId, includesClosedAccounts: true);

            var closingAccount = userAccountsList.FirstOrDefault(account => account.Id == accountId)
                ?? throw new InvalidOperationException("Account not found.");

            if (closingAccount.AccountStatus == "Closed")
                throw new InvalidOperationException("Account already closed.");


            var destinationAccount = userAccountsList.FirstOrDefault(account => account.Id == destinationAccountId)
                ?? throw new InvalidOperationException("Destination account not found.");

            if (destinationAccount.AccountStatus == "Closed")
                throw new InvalidOperationException("Cannot transfer to a closed account.");

            decimal earlyClosurePenalty = 0;
            if (closingAccount.SavingsType == "FixedDeposit" &&
                closingAccount.MaturityDate.HasValue &&
                closingAccount.MaturityDate > DateTime.UtcNow)
            {
                earlyClosurePenalty = closingAccount.Balance * DECIMAL_EARLY_CLOSURE_PENALTY;
            }

            decimal transferAmount = closingAccount.Balance - earlyClosurePenalty;

            return await savingsRepository.CloseSavingsAccountAsync(accountId, destinationAccountId, transferAmount, earlyClosurePenalty);
        }


        public async Task<WithdrawResponseDto> WithdrawAsync(int accountId, decimal amount, string destinationLabel, int userId)
        {
            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be positive.");

            var userAccountsList = await savingsRepository.GetSavingsAccountsByUserIdAsync(userId, includesClosedAccounts: true);
            var destinationAccount = userAccountsList.Find(account => account.Id == accountId)
                ?? throw new InvalidOperationException("Account not found or does not belong to you.");

            if (destinationAccount.AccountStatus == "Closed")
                throw new InvalidOperationException("Cannot withdraw from a closed account.");

            if (destinationAccount.Balance < amount)
                throw new InvalidOperationException("Insufficient balance.");

            decimal earlyWithdrawalPenalty = 0;
            if (destinationAccount.SavingsType == "FixedDeposit" &&
                destinationAccount.MaturityDate.HasValue &&
                destinationAccount.MaturityDate.Value > DateTime.UtcNow)
            {
                earlyWithdrawalPenalty = amount * DECIMAL_EARLY_WITHDRAWAL_PENALTY;
            }

            decimal totalSumToWithdraw = amount + earlyWithdrawalPenalty;
            if (totalSumToWithdraw > destinationAccount.Balance)
                throw new InvalidOperationException("Insufficient balance after penalty.");

            return await savingsRepository.WithdrawAsync(accountId, totalSumToWithdraw, destinationLabel, earlyWithdrawalPenalty);
        }


        public Task<AutoDeposit?> GetAutoDepositAsync(int accountId)
            => savingsRepository.GetAutoDepositAsync(accountId);


        public Task SaveAutoDepositAsync(AutoDeposit autoDeposit)
            => savingsRepository.SaveAutoDepositAsync(autoDeposit);


        public Task<List<FundingSourceOption>> GetFundingSourcesAsync(int userId)
        {
            return savingsRepository.GetFundingSourcesAsync(userId);
        }


        public async Task<(List<SavingsTransaction> Items, int TotalCount)> GetTransactionsAsync(
        int accountId,
        string filter,
        int page,
        int pageSize)
        {
            if (page <= 0)
                throw new ArgumentException("Page must be >= 1");

            if (pageSize <= 0 || pageSize > 100)
                pageSize = 20;

            return await savingsRepository.GetTransactionsPagedAsync(
                accountId,
                filter,
                page,
                pageSize);
        }

        public async Task<List<SavingsAccount>> GetValidTransferDestinationsAsync(int currentAccountId)
        {
            var openAccountsList = await savingsRepository.GetSavingsAccountsByUserIdAsync(currentAccountId, includesClosedAccounts: false);
            return openAccountsList.Where(account => account.Id != currentAccountId).ToList();
        }

        public decimal ComputeWithdrawalPenalty(decimal amount)
        {
            return amount * DECIMAL_EARLY_WITHDRAWAL_PENALTY;
        }

        public bool HasRiskEarlyWithdrawal(SavingsAccount savingsAccount)
        {
            return savingsAccount?.SavingsType == "FixedDeposit" &&
            savingsAccount.MaturityDate.HasValue &&
            savingsAccount.MaturityDate.Value > DateTime.UtcNow;
        }

        public decimal GetPenaltyDecimalFor(string penaltyCase) 
        {
            return penaltyCase switch
            {
                "EarlyWithdrawal" => DECIMAL_EARLY_WITHDRAWAL_PENALTY,
                "EarlyClosure" => DECIMAL_EARLY_CLOSURE_PENALTY,
                _ => throw new ArgumentException("Invalid penalty case.")
            };
        }
    }
}
