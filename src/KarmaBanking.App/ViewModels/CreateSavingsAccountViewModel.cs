using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;

namespace KarmaBanking.App.ViewModels
{
    public class CreateSavingsAccountViewModel : INotifyPropertyChanged
    {
        private readonly ISavingsService savingsService;

        private string selectedSavingsType = "Flexible";
        private string accountNickname = string.Empty;
        private string initialDepositAmountText = string.Empty;
        private int? selectedFundingAccountId;
        private string errorMessage = string.Empty;
        private bool isAccountCreated;
        private bool isSubmitting;

        public CreateSavingsAccountViewModel(ISavingsService savingsService)
        {
            this.savingsService = savingsService;
            CreateAccountCommand = new RelayCommand(SubmitCreateAccountFormAsync, CanSubmitCreateAccountForm);
        }

        public string SelectedSavingsType
        {
            get => selectedSavingsType;
            set { selectedSavingsType = value; OnPropertyChanged(); }
        }

        public string AccountNickname
        {
            get => accountNickname;
            set { accountNickname = value; OnPropertyChanged(); }
        }

        public string InitialDepositAmountText
        {
            get => initialDepositAmountText;
            set { initialDepositAmountText = value; OnPropertyChanged(); }
        }

        public int? SelectedFundingAccountId
        {
            get => selectedFundingAccountId;
            set { selectedFundingAccountId = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set { errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsAccountCreated
        {
            get => isAccountCreated;
            set { isAccountCreated = value; OnPropertyChanged(); }
        }

        public bool IsSubmitting
        {
            get => isSubmitting;
            set { isSubmitting = value; OnPropertyChanged(); }
        }

        public ICommand CreateAccountCommand { get; }

        private bool CanSubmitCreateAccountForm()
        {
            return !isSubmitting
                && !string.IsNullOrWhiteSpace(accountNickname)
                && !string.IsNullOrWhiteSpace(initialDepositAmountText)
                && selectedFundingAccountId != null;
        }

        private async Task SubmitCreateAccountFormAsync()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(initialDepositAmountText, out decimal initialDepositAmount) || initialDepositAmount <= 0)
            {
                ErrorMessage = "Initial deposit must be a positive number.";
                return;
            }

            IsSubmitting = true;

            SavingsAccount newSavingsAccount = new SavingsAccount
            {
                UserId = 1,
                SavingsType = selectedSavingsType,
                AccountName = accountNickname,
                Balance = initialDepositAmount,
                FundingAccountId = selectedFundingAccountId,
                Apy = 0
            };

            bool wasCreated = await savingsService.CreateSavingsAccountAsync(newSavingsAccount);

            IsSubmitting = false;

            if (wasCreated)
                IsAccountCreated = true;
            else
                ErrorMessage = "Failed to create account. Please check your details and try again.";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
