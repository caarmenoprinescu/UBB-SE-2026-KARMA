using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KarmaBanking.App.Models;
using KarmaBanking.App.Models.DTOs;
using KarmaBanking.App.Models.Enums;
using KarmaBanking.App.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private ObservableCollection<SavingsAccount> savingsAccounts = [];

        [ObservableProperty] private string totalSavedAmount = "$0.00";
        [ObservableProperty] private string numberOfAccountsText = "across 0 accounts";
        [ObservableProperty] private string bestInterestRate = "0.00%";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LivePreview))]
        [NotifyPropertyChangedFor(nameof(WithdrawHasEarlyRisk))]
        [NotifyPropertyChangedFor(nameof(WithdrawPenaltySummary))]
        [NotifyPropertyChangedFor(nameof(WithdrawEstimatedPenalty))]
        [NotifyPropertyChangedFor(nameof(WithdrawNetAmount))]
        [NotifyPropertyChangedFor(nameof(WithdrawHasPenalty))]
        [NotifyPropertyChangedFor(nameof(CloseHasPenalty))]
        private SavingsAccount? selectedAccount;

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
        [ObservableProperty] private ObservableCollection<FundingSourceOption> fundingSources = [];
        [ObservableProperty] private string selectedFrequency = string.Empty;

        public bool IsGoalSavings => SelectedSavingsType == "GoalSavings";
        public Dictionary<string, string> FieldErrors { get; } = [];

        // ── Deposit ──────────────────────────────────────────────────────────

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LivePreview))]
        private string depositAmountText = string.Empty;

        [ObservableProperty] private string depositSource = string.Empty;
        [ObservableProperty] private bool showDepositSuccess;
        [ObservableProperty] private string depositSuccessMessage = string.Empty;

        private CancellationTokenSource? depositCancelationTokenSource;

        public string LivePreview
        {
            get
            {
                if (decimal.TryParse(DepositAmountText, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal amount)
                    && amount > 0 && SelectedAccount != null)
                {
                    return $"New balance will be: ${(SelectedAccount.Balance + amount):N2}";
                }

                return string.Empty;
            }
        }

        // ── Withdraw Panel ───────────────────────────────────────────────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WithdrawEstimatedPenalty))]
        [NotifyPropertyChangedFor(nameof(WithdrawNetAmount))]
        [NotifyPropertyChangedFor(nameof(WithdrawHasPenalty))]
        private string withdrawAmountText = string.Empty;

        [ObservableProperty] private FundingSourceOption? withdrawDestination;
        [ObservableProperty] private string withdrawResultMessage = string.Empty;
        [ObservableProperty] private bool withdrawSuccess;

        public bool WithdrawHasEarlyRisk => savingsService.HasRiskEarlyWithdrawal(SelectedAccount);

        public decimal WithdrawEstimatedPenalty
        {
            get
            {
                if (!WithdrawHasEarlyRisk)
                {
                    return 0;
                }

                if (!decimal.TryParse(WithdrawAmountText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal withdrawAmount) || withdrawAmount <= 0)
                {
                    return 0;
                }

                return savingsService.ComputeWithdrawalPenalty(withdrawAmount);
            }
        }

        public decimal WithdrawNetAmount
        {
            get
            {
                if (!decimal.TryParse(WithdrawAmountText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal withdrawAmount) || withdrawAmount <= 0)
                {
                    return 0;
                }

                return withdrawAmount - WithdrawEstimatedPenalty;
            }
        }

        public bool WithdrawHasPenalty => WithdrawEstimatedPenalty > 0;

        public string WithdrawPenaltySummary =>
            WithdrawHasEarlyRisk ? $"Early withdrawal penalty: {savingsService.GetPenaltyDecimalFor("EarlyWithdrawal"):P2} of amount. Maturity date: {SelectedAccount?.MaturityDate:d}" : string.Empty;

        public async Task<bool> ConfirmWithdrawAsync()
        {
            WithdrawResultMessage = string.Empty;
            WithdrawSuccess = false;
            if (!decimal.TryParse(WithdrawAmountText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            { WithdrawResultMessage = "Please enter a valid amount."; return false; }
            if (WithdrawDestination == null)
            { WithdrawResultMessage = "Please select a destination account."; return false; }
            IsLoading = true;
            try
            {
                var withdrawResponseDto = await savingsService.WithdrawAsync(SelectedAccount!.Id, amount, WithdrawDestination.DisplayName, CurrentUserId);
                WithdrawSuccess = withdrawResponseDto.Success;
                WithdrawResultMessage = withdrawResponseDto.Success
                    ? $"Withdrawn: ${withdrawResponseDto.AmountWithdrawn:N2}" + (withdrawResponseDto.PenaltyApplied > 0 ? $" (penalty: ${withdrawResponseDto.PenaltyApplied:N2})" : "") + $". New balance: ${withdrawResponseDto.NewBalance:N2}"
                    : withdrawResponseDto.Message;
                if (withdrawResponseDto.Success)
                {
                    WithdrawAmountText = string.Empty;
                    await LoadAccountsAsync();
                }
                return withdrawResponseDto.Success;
            }
            catch (Exception exception) { WithdrawResultMessage = exception.Message; return false; }
            finally { IsLoading = false; }
        }

        // ── Auto Deposit ─────────────────────────────────────────────────────

        private AutoDeposit? currentAutoDeposit;

        [ObservableProperty] private string autoDepositAmountText = string.Empty;
        [ObservableProperty] private string autoDepositFrequency = string.Empty;
        [ObservableProperty] private DateTimeOffset? autoDepositStartDate = DateTimeOffset.Now.AddDays(1);
        [ObservableProperty] private bool autoDepositIsActive = true;
        [ObservableProperty] private bool hasExistingAutoDeposit;
        public string ExistingLabel => HasExistingAutoDeposit ? "Modify" : "Set Up";
        [ObservableProperty] private string autoDepositSaveMessage = string.Empty;

        public async Task LoadAutoDepositAsync(int accountId)
        {
            AutoDepositSaveMessage = string.Empty;
            currentAutoDeposit = await savingsService.GetAutoDepositAsync(accountId);
            if (currentAutoDeposit != null)
            {
                HasExistingAutoDeposit = true;
                AutoDepositAmountText = currentAutoDeposit.Amount.ToString(CultureInfo.InvariantCulture);
                AutoDepositFrequency = currentAutoDeposit.Frequency.ToString();
                AutoDepositStartDate = new DateTimeOffset(currentAutoDeposit.NextRunDate);
                AutoDepositIsActive = currentAutoDeposit.IsActive;
            }
            else
            {
                HasExistingAutoDeposit = false;
                AutoDepositAmountText = string.Empty;
                AutoDepositFrequency = string.Empty;
                AutoDepositStartDate = DateTimeOffset.Now.AddDays(1);
                AutoDepositIsActive = true;
            }
        }

        public async Task SaveAutoDepositAsync()
        {
            ErrorMessage = string.Empty;
            AutoDepositSaveMessage = string.Empty;

            if (!decimal.TryParse(AutoDepositAmountText, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal amount) || amount <= 0)
            { ErrorMessage = "Auto deposit amount must be positive."; return; }

            if (string.IsNullOrWhiteSpace(AutoDepositFrequency))
            { ErrorMessage = "Please select a frequency."; return; }

            if (!Enum.TryParse<DepositFrequency>(AutoDepositFrequency, out var freq))
            { ErrorMessage = "Invalid frequency."; return; }

            var autoDeposit = new AutoDeposit
            {
                Id = currentAutoDeposit?.Id ?? 0,
                SavingsAccountId = SelectedAccount!.Id,
                Amount = amount,
                Frequency = freq,
                NextRunDate = AutoDepositStartDate?.DateTime ?? DateTime.Now.AddDays(1),
                IsActive = AutoDepositIsActive
            };

            await savingsService.SaveAutoDepositAsync(autoDeposit);
            AutoDepositSaveMessage = "Auto deposit saved successfully.";
            await LoadAutoDepositAsync(SelectedAccount.Id);
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
                var accountsList = await savingsService.GetAccountsAsync(CurrentUserId);
                SavingsAccounts.Clear();
                foreach (var account in accountsList)
                {
                    SavingsAccounts.Add(account);
                }

                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(ShowAccountsList));

                TotalSavedAmount = $"${SavingsAccounts.Sum(account => account.Balance):F2}";
                NumberOfAccountsText = $"across {SavingsAccounts.Count} account{(SavingsAccounts.Count == 1 ? "" : "s")}";
                decimal bestApy = SavingsAccounts.Any() ? SavingsAccounts.Max(account => account.Apy) : 0;
                BestInterestRate = $"{bestApy * 100:F2}%";
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        public async Task CloseAccountAsync(SavingsAccount account)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var closureResultDto = await savingsService.CloseAccountAsync(account.Id, CurrentUserId, 1);
                bool ok = closureResultDto.Success;
                if (!ok) { ErrorMessage = "Failed to close account."; return; }
                await LoadAccountsAsync();
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
            finally { IsLoading = false; }
        }

        // ── Close Account Panel ──────────────────────────────────────────────
        [ObservableProperty] private ObservableCollection<SavingsAccount> closeDestinationAccounts = [];

        private int selectedCloseDestinationId;
        public int SelectedCloseDestinationId
        {
            get => selectedCloseDestinationId;
            set { selectedCloseDestinationId = value; OnPropertyChanged(); }
        }

        private bool closeUserConfirmed;
        public bool CloseUserConfirmed
        {
            get => closeUserConfirmed;
            set { closeUserConfirmed = value; OnPropertyChanged(); }
        }

        [ObservableProperty] private string closeResultMessage = string.Empty;
        [ObservableProperty] private bool closeSuccess;

        public bool CloseHasPenalty =>
            SelectedAccount?.SavingsType == "FixedDeposit" &&
            SelectedAccount.MaturityDate.HasValue &&
            SelectedAccount.MaturityDate.Value > DateTime.UtcNow;

        public async Task LoadCloseDestinationAccountsAsync()
        {
            CloseUserConfirmed = false;
            CloseResultMessage = string.Empty;
            CloseSuccess = false;
            var openAccountsList = await savingsService.GetValidTransferDestinationsAsync(SelectedAccount!.Id);
            CloseDestinationAccounts.Clear();
            foreach (var account in openAccountsList)
            {
                CloseDestinationAccounts.Add(account);
            }

            if (CloseDestinationAccounts.Count > 0)
            {
                SelectedCloseDestinationId = CloseDestinationAccounts[0].Id;
            }

            OnPropertyChanged(nameof(CloseHasPenalty));
        }

        public async Task<bool> ConfirmCloseAsync()
        {
            if (!CloseUserConfirmed) { CloseResultMessage = "Please confirm account closure."; return false; }
            if (SelectedCloseDestinationId == 0) { CloseResultMessage = "Please select a destination account."; return false; }
            IsLoading = true;
            try
            {
                var result = await savingsService.CloseAccountAsync(SelectedAccount!.Id, SelectedCloseDestinationId, CurrentUserId);
                CloseSuccess = result.Success;
                CloseResultMessage = result.Success ? "Account closed successfully." : result.Message;
                if (result.Success)
                {
                    await LoadAccountsAsync();
                }

                return result.Success;
            }
            catch (Exception exception) { CloseResultMessage = exception.Message; return false; }
            finally { IsLoading = false; }
        }

        // ── Commands: Create Account ─────────────────────────────────────────

        public async Task LoadFundingSourcesAsync()
        {
            try
            {
                var fundingSourcesList = await savingsService.GetFundingSourcesAsync(CurrentUserId);
                FundingSources.Clear();
                foreach (var fundingSource in fundingSourcesList)
                {
                    FundingSources.Add(fundingSource);
                }

                if (FundingSources.Count > 0)
                {
                    SelectedFundingSource = FundingSources[0];
                }
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
        }

        [RelayCommand]
        public async Task CreateAccountAsync()
        {
            FieldErrors.Clear();
            ErrorMessage = string.Empty;
            ShowCreateConfirmation = false;

            if (string.IsNullOrWhiteSpace(SelectedSavingsType))
            {
                FieldErrors["SavingsType"] = "Please select an account type.";
            }

            if (string.IsNullOrWhiteSpace(AccountName))
            {
                FieldErrors["AccountName"] = "Account name is required.";
            }

            if (!decimal.TryParse(InitialDepositText, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out decimal deposit) || deposit <= 0)
            {
                FieldErrors["InitialDeposit"] = "Initial deposit must be a positive number.";
            }

            if (SelectedFundingSource == null)
            {
                FieldErrors["FundingSource"] = "Please select a funding source.";
            }

            if (string.IsNullOrWhiteSpace(SelectedFrequency))
            {
                FieldErrors["Frequency"] = "Please select a deposit frequency.";
            }

            if (IsGoalSavings)
            {
                if (!TargetAmount.HasValue || TargetAmount.Value <= 0)
                {
                    FieldErrors["TargetAmount"] = "Target amount is required.";
                }

                if (!TargetDate.HasValue)
                {
                    FieldErrors["TargetDate"] = "Target date is required.";
                }
                else if (TargetDate.Value.Date <= DateTime.Today)
                {
                    FieldErrors["TargetDate"] = "Target date must be in the future.";
                }
            }

            OnPropertyChanged(nameof(FieldErrors));
            if (FieldErrors.Count > 0)
            {
                return;
            }

            IsLoading = true;
            try
            {
                var createSavingsAccountDto = new CreateSavingsAccountDto
                {
                    UserId = CurrentUserId,
                    SavingsType = SelectedSavingsType,
                    AccountName = AccountName.Trim(),
                    InitialDeposit = deposit,
                    FundingAccountId = SelectedFundingSource!.Id,
                    TargetAmount = IsGoalSavings ? TargetAmount : null,
                    TargetDate = IsGoalSavings ? TargetDate?.DateTime : null,
                    MaturityDate = MaturityDate?.DateTime,
                    DepositFrequency = Enum.TryParse<DepositFrequency>(SelectedFrequency, out var selectedFrequency) ? selectedFrequency : null
                };
                await savingsService.CreateAccountAsync(createSavingsAccountDto);
                ShowCreateConfirmation = true;
                ResetCreateForm();
                await LoadAccountsAsync();
            }
            catch (Exception exception) { ErrorMessage = exception.Message; }
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

            depositCancelationTokenSource?.Cancel();
            depositCancelationTokenSource = new CancellationTokenSource();

            IsLoading = true;
            try
            {
                var depositResponseDto = await savingsService.DepositAsync(
                    SelectedAccount.Id, amount, DepositSource, CurrentUserId);

                DepositSuccessMessage = $"Deposit successful! New balance: ${depositResponseDto.NewBalance:N2}";
                ShowDepositSuccess = true;
                DepositAmountText = string.Empty;
                await LoadAccountsAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception) { ErrorMessage = exception.Message; }
            finally { IsLoading = false; }
        }

        public void CancelDeposit() => depositCancelationTokenSource?.Cancel();

        [ObservableProperty]
        private ObservableCollection<SavingsTransaction> transactions = [];

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
                {
                    transactions.Add(tx);
                }

                totalPages = (int)Math.Ceiling((double)result.TotalCount / 10);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
        public async Task NextPage(int accountId)
        {
            if (currentPage >= totalPages)
            {
                return;
            }

            currentPage++;
            await LoadTransactionsAsync(accountId);
        }

        public async Task PreviousPage(int accountId)
        {
            if (currentPage <= 1)
            {
                return;
            }

            currentPage--;
            await LoadTransactionsAsync(accountId);
        }
        public async Task ChangeFilter(int accountId, string filter)
        {
            selectedFilter = filter;
            currentPage = 1;
            await LoadTransactionsAsync(accountId);
        }

        public DateTimeOffset? MaturityDate { get; set; }
    }
}
