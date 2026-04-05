using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services.Interfaces;
using KarmaBanking.App.Utils;
using Microsoft.UI.Xaml;
using KarmaBanking.App.Models.DTOs;

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
            set { accountNickname = value; OnPropertyChanged(); CreateAccountCommand.RaiseCanExecuteChanged(); }
        }

        public string InitialDepositAmountText
        {
            get => initialDepositAmountText;
            set { initialDepositAmountText = value; OnPropertyChanged(); CreateAccountCommand.RaiseCanExecuteChanged(); }
        }

        public int? SelectedFundingAccountId
        {
            get => selectedFundingAccountId;
            set { selectedFundingAccountId = value; OnPropertyChanged(); CreateAccountCommand.RaiseCanExecuteChanged(); }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ErrorMessageVisibility));
            }
        }

        public Visibility ErrorMessageVisibility =>
            string.IsNullOrEmpty(errorMessage) ? Visibility.Collapsed : Visibility.Visible;

        public bool IsAccountCreated
        {
            get => isAccountCreated;
            set
            {
                isAccountCreated = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SuccessMessageVisibility));
            }
        }

        public Visibility SuccessMessageVisibility =>
            isAccountCreated ? Visibility.Visible : Visibility.Collapsed;

        public bool IsSubmitting
        {
            get => isSubmitting;
            set { isSubmitting = value; OnPropertyChanged(); }
        }

        public RelayCommand CreateAccountCommand { get; }

        private bool CanSubmitCreateAccountForm()
        {
            return !isSubmitting
                && !string.IsNullOrWhiteSpace(accountNickname)
                && !string.IsNullOrWhiteSpace(initialDepositAmountText)
                && selectedFundingAccountId != null;
        }

        public async Task SubmitCreateAccountFormAsync()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(initialDepositAmountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal initialDepositAmount) || initialDepositAmount <= 0)
            {
                ErrorMessage = "Initial deposit must be a positive number.";
                return;
            }

            IsSubmitting = true;

            var dto = new CreateSavingsAccountDto
            {
                UserId = 1,
                SavingsType = selectedSavingsType,
                AccountName = accountNickname,
                InitialDeposit = initialDepositAmount,
                FundingAccountId = selectedFundingAccountId.Value
            };

            var createdAccount = await savingsService.CreateAccountAsync(dto);

            IsSubmitting = false;

            if (createdAccount != null)
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
