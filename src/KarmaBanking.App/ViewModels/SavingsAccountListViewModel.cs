using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using Microsoft.UI.Xaml;

namespace KarmaBanking.App.ViewModels
{
    public class SavingsAccountListViewModel : INotifyPropertyChanged
    {
        private readonly ISavingsService savingsService;

        private string totalSavedAmount = "$0.00";
        private string numberOfAccountsText = "across 0 accounts";
        private string bestInterestRate = "0.00%";
        private Visibility emptyStateVisibility = Visibility.Visible;
        private Visibility accountsListVisibility = Visibility.Collapsed;

        public SavingsAccountListViewModel(ISavingsService savingsService)
        {
            this.savingsService = savingsService;
            SavingsAccounts = new ObservableCollection<SavingsAccount>();
        }

        public ObservableCollection<SavingsAccount> SavingsAccounts { get; }

        public string TotalSavedAmount
        {
            get => totalSavedAmount;
            set { totalSavedAmount = value; OnPropertyChanged(); }
        }

        public string NumberOfAccountsText
        {
            get => numberOfAccountsText;
            set { numberOfAccountsText = value; OnPropertyChanged(); }
        }

        public string BestInterestRate
        {
            get => bestInterestRate;
            set { bestInterestRate = value; OnPropertyChanged(); }
        }

        public Visibility EmptyStateVisibility
        {
            get => emptyStateVisibility;
            set { emptyStateVisibility = value; OnPropertyChanged(); }
        }

        public Visibility AccountsListVisibility
        {
            get => accountsListVisibility;
            set { accountsListVisibility = value; OnPropertyChanged(); }
        }

        public string LoadErrorMessage { get; private set; } = string.Empty;

        public async Task LoadSavingsAccountsAsync(int userId)
        {
            List<SavingsAccount> accounts;
            try
            {
                accounts = await savingsService.GetAccountsAsync(userId, includesClosed: false);
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
                return;
            }

            SavingsAccounts.Clear();
            foreach (var account in accounts)
                SavingsAccounts.Add(account);

            EmptyStateVisibility = SavingsAccounts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            AccountsListVisibility = SavingsAccounts.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            TotalSavedAmount = $"${SavingsAccounts.Sum(account => account.Balance):F2}";
            NumberOfAccountsText = $"across {SavingsAccounts.Count} account{(SavingsAccounts.Count == 1 ? "" : "s")}";

            decimal highestApy = SavingsAccounts.Any() ? SavingsAccounts.Max(account => account.Apy) : 0;
            BestInterestRate = $"{highestApy:F2}%";
        }

        public async Task CloseSavingsAccountAsync(int accountId)
        {
            try
            {
                var result = await savingsService.CloseAccountAsync(accountId, 1, 1);
                bool success = result.Success;

                if (!success)
                {
                    LoadErrorMessage = "Failed to close account.";
                    return;
                }

                await LoadSavingsAccountsAsync(userId: 1);
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task DepositAsync(int accountId, decimal amount)
        {
            try
            {
                await savingsService.DepositAsync(accountId, amount, "Manual", 1);

                // Refresh accounts after deposit
                await LoadSavingsAccountsAsync(userId: 1);
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
            }
        }

        public async Task ProcessSchedulesAsync()
        {
            try
            {
                await LoadSavingsAccountsAsync(1);
            }
            catch (Exception ex)
            {
                LoadErrorMessage = ex.Message;
            }
        }

    }
}
