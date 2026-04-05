using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace KarmaBanking.App.ViewModels
{
    public partial class SavingsViewModel : BaseViewModel
    {
        private readonly ISavingsService savingsService;
        private const int CurrentUserId = 1;

        // ── My Accounts ──────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEmpty))]
        [NotifyPropertyChangedFor(nameof(ShowAccountsList))]
        private ObservableCollection<SavingsAccount> savingsAccounts = new();

        [ObservableProperty] private string totalSavedAmount = "$0.00";
        [ObservableProperty] private string numberOfAccountsText = "across 0 accounts";
        [ObservableProperty] private string bestInterestRate = "0.00%";
        [ObservableProperty] private SavingsAccount? selectedAccount;

        public bool IsEmpty => SavingsAccounts.Count == 0;
        public bool ShowAccountsList => SavingsAccounts.Count > 0;

        // ── Create Account ───────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsGoalSavings))]
        private string selectedSavingsType = string.Empty;

        [ObservableProperty] private string accountName = string.Empty;
        [ObservableProperty] private string initialDepositText = string.Empty;
        [ObservableProperty] private FundingSourceOption? selectedFundingSource;
        [ObservableProperty] private decimal? targetAmount;
        [ObservableProperty] private DateTimeOffset? targetDate;
        [ObservableProperty] private bool showCreateConfirmation;
        [ObservableProperty] private ObservableCollection<FundingSourceOption> fundingSources = new();

        public bool IsGoalSavings => SelectedSavingsType == "GoalSavings";
        public Dictionary<string, string> FieldErrors { get; } = new();

        // ── Deposit ──────────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LivePreview))]
        private string depositAmountText = string.Empty;

        [ObservableProperty] private string depositSource = string.Empty;
        [ObservableProperty] private bool showDepositSuccess;
        [ObservableProperty] private string depositSuccessMessage = string.Empty;

        private CancellationTokenSource? depositCts;

        public string LivePreview
        {
            get
            {
                if (decimal.TryParse(DepositAmountText, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal amount)
                    && amount > 0 && SelectedAccount != null)
                    return $"New balance will be: {(SelectedAccount.Balance + amount):C2}";
                return string.Empty;
            }
        }

        // ── Constructor ──────────────────────────────────────────────────────

        public SavingsViewModel(ISavingsService savingsService)
        {
            this.savingsService = savingsService;
        }

        // ── Commands: My Accounts ────────────────────────────────────────────

        [RelayCommand]
        public async Task LoadAccountsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var accounts = await savingsService.GetAccountsAsync(CurrentUserId);
                SavingsAccounts.Clear();
                foreach (var a in accounts) SavingsAccounts.Add(a);

                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(ShowAccountsList));

                TotalSavedAmount = $"${SavingsAccounts.Sum(a => a.Balance):F2}";
                NumberOfAccountsText = $"across {SavingsAccounts.Count} account{(SavingsAccounts.Count == 1 ? "" : "s")}";
                decimal best = SavingsAccounts.Any() ? SavingsAccounts.Max(a => a.Apy) : 0;
                BestInterestRate = $"{best * 100:F2}%";
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public async Task CloseAccountAsync(SavingsAccount account)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                bool ok = await savingsService.CloseAccountAsync(account.Id, CurrentUserId, 1);
                if (!ok) { ErrorMessage = "Failed to close account."; return; }
                await LoadAccountsAsync();
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsLoading = false; }
        }

        // ── Commands: Create Account ─────────────────────────────────────────

        public async Task LoadFundingSourcesAsync()
        {
            try
            {
                var sources = await savingsService.GetFundingSourcesAsync(CurrentUserId);
                FundingSources.Clear();
                foreach (var s in sources) FundingSources.Add(s);
                if (FundingSources.Count > 0) SelectedFundingSource = FundingSources[0];
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
        }

        [RelayCommand]
        public async Task CreateAccountAsync()
        {
            FieldErrors.Clear();
            ErrorMessage = string.Empty;
            ShowCreateConfirmation = false;

            if (string.IsNullOrWhiteSpace(SelectedSavingsType))
                FieldErrors["SavingsType"] = "Please select an account type.";
            if (string.IsNullOrWhiteSpace(AccountName))
                FieldErrors["AccountName"] = "Account name is required.";
            if (!decimal.TryParse(InitialDepositText, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal deposit) || deposit <= 0)
                FieldErrors["InitialDeposit"] = "Initial deposit must be a positive number.";
            if (SelectedFundingSource == null)
                FieldErrors["FundingSource"] = "Please select a funding source.";
            if (IsGoalSavings)
            {
                if (!TargetAmount.HasValue || TargetAmount.Value <= 0)
                    FieldErrors["TargetAmount"] = "Target amount is required.";
                if (!TargetDate.HasValue)
                    FieldErrors["TargetDate"] = "Target date is required.";
                else if (TargetDate.Value.Date <= DateTime.Today)
                    FieldErrors["TargetDate"] = "Target date must be in the future.";
            }

            OnPropertyChanged(nameof(FieldErrors));
            if (FieldErrors.Count > 0) return;

            IsLoading = true;
            try
            {
                var dto = new CreateSavingsAccountDto
                {
                    UserId = CurrentUserId,
                    SavingsType = SelectedSavingsType,
                    AccountName = AccountName.Trim(),
                    InitialDeposit = deposit,
                    FundingAccountId = SelectedFundingSource!.Id,
                    TargetAmount = IsGoalSavings ? TargetAmount : null,
                    TargetDate = IsGoalSavings ? TargetDate?.DateTime : null
                };
                await savingsService.CreateAccountAsync(dto);
                ShowCreateConfirmation = true;
                ResetCreateForm();
                await LoadAccountsAsync();
            }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsLoading = false; }
        }

        private void ResetCreateForm()
        {
            AccountName = string.Empty;
            InitialDepositText = string.Empty;
            SelectedSavingsType = string.Empty;
            TargetAmount = null;
            TargetDate = null;
            FieldErrors.Clear();
        }

        // ── Commands: Deposit ────────────────────────────────────────────────

        [RelayCommand]
        public async Task DepositAsync()
        {
            ErrorMessage = string.Empty;
            ShowDepositSuccess = false;

            if (SelectedAccount == null) { ErrorMessage = "No account selected."; return; }

            if (!decimal.TryParse(DepositAmountText, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            {
                ErrorMessage = "Please enter a valid positive amount.";
                return;
            }

            depositCts?.Cancel();
            depositCts = new CancellationTokenSource();

            IsLoading = true;
            try
            {
                var response = await savingsService.DepositAsync(
                    SelectedAccount.Id, amount, DepositSource, CurrentUserId);

                DepositSuccessMessage = $"Deposit successful! New balance: {response.NewBalance:C2}";
                ShowDepositSuccess = true;
                DepositAmountText = string.Empty;
                await LoadAccountsAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ErrorMessage = ex.Message; }
            finally { IsLoading = false; }
        }

        public void CancelDeposit() => depositCts?.Cancel();

        [ObservableProperty]
        private ObservableCollection<SavingsTransaction> transactions = new();

        [ObservableProperty]
        private int currentPage = 1;

        [ObservableProperty]
        private int totalPages;

        [ObservableProperty]
        private string selectedFilter = "All";

        public async Task LoadTransactionsAsync(int accountId)
        {
            try
            {
                var result = await savingsService.GetTransactionsAsync(
                    accountId,
                    selectedFilter,
                    currentPage,
                    10);

                transactions.Clear();

                foreach (var tx in result.Items)
                    transactions.Add(tx);

                totalPages = (int)Math.Ceiling((double)result.TotalCount / 10);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
        public async Task NextPage(int accountId)
        {
            if (currentPage >= totalPages) return;

            currentPage++;
            await LoadTransactionsAsync(accountId);
        }

        public async Task PreviousPage(int accountId)
        {
            if (currentPage <= 1) return;

            currentPage--;
            await LoadTransactionsAsync(accountId);
        }
        public async Task ChangeFilter(int accountId, string filter)
        {
            selectedFilter = filter;
            currentPage = 1;
            await LoadTransactionsAsync(accountId);
        }
    }
}
