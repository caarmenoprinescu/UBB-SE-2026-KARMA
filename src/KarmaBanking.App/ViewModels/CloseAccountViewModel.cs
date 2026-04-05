using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KarmaBanking.App.ViewModels
{
    public class CloseAccountViewModel
    {
        private readonly ISavingsService _service;
        private const int CurrentUserId = 1;

        public SavingsAccount Account { get; }

        public ObservableCollection<SavingsAccount> DestinationAccounts { get; } = new();

        public int SelectedDestinationAccountId { get; set; }

        public bool UserConfirmed { get; set; }

        public CloseAccountViewModel(ISavingsService service, SavingsAccount account)
        {
            _service = service;
            Account = account;
        }

        public async Task LoadAccountsAsync()
        {
            var accounts = await _service.GetAccountsAsync(CurrentUserId);

            DestinationAccounts.Clear();

            foreach (var acc in accounts.Where(a => a.Id != Account.Id))
            {
                DestinationAccounts.Add(acc);
            }

            // auto-select first if exists
            if (DestinationAccounts.Count > 0)
                SelectedDestinationAccountId = DestinationAccounts[0].Id;
        }

        public async Task<bool> CloseAsync()
        {
            if (!UserConfirmed)
                return false;

            if (SelectedDestinationAccountId == 0)
                return false;

            return await _service.CloseAccountAsync(
                Account.Id,
                SelectedDestinationAccountId,
                CurrentUserId);
        }

        public decimal EstimatedPenalty =>
        Account.SavingsType == "FixedDeposit" &&
        Account.MaturityDate.HasValue &&
        Account.MaturityDate > DateTime.UtcNow
            ? Account.Balance * 0.02m
            : 0;

        public decimal EstimatedTransfer => Account.Balance - EstimatedPenalty;
    }
}