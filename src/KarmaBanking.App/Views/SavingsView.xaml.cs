using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Globalization;

namespace KarmaBanking.App.Views
{
    public sealed partial class SavingsView : Page
    {
        private readonly SavingsViewModel viewModel;


        public SavingsView()
        {
            InitializeComponent();
            var repository = new SavingsRepository();
            var service = new SavingsService(repository);
            viewModel = new SavingsViewModel(service);
            DataContext = viewModel;
            MainNavigationView.SelectedItem = MyAccountsTab;
        }


        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
            await viewModel.LoadAccountsAsync();

            if (viewModel.HasError)
                await ShowDialogAsync("Load Error", viewModel.ErrorMessage);
        }

        // ── Tab switching ────────────────────────────────────────────────────

        private async void OnTabSelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem tab) return;
            string tag = tab.Tag?.ToString() ?? string.Empty;

            MyAccountsPanel.Visibility = tag == "MyAccounts" ? Visibility.Visible : Visibility.Collapsed;
            OpenNewPanel.Visibility = tag == "OpenNew" ? Visibility.Visible : Visibility.Collapsed;
            ManagePanel.Visibility = tag == "Manage" ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "OpenNew")
            {
                SavingsTypeRadioButtons.SelectedIndex = -1;
                FrequencyRadioButtons.SelectedIndex = -1;
                viewModel.SelectedSavingsType = string.Empty;
                viewModel.SelectedFrequency = string.Empty;
                GoalSavingsPanel.Visibility = Visibility.Collapsed;
                ClearCreateErrors();

                await viewModel.LoadFundingSourcesAsync();
                FundingSourceComboBox.ItemsSource = viewModel.FundingSources;
                if (viewModel.FundingSources.Count > 0)
                    FundingSourceComboBox.SelectedIndex = 0;
            }

            if (tag == "Manage")
            {
                HideAllActionPanels();
                ManageButtonsPanel.Visibility = Visibility.Collapsed;
                ManageAccountComboBox.SelectedIndex = -1;
            }
        }

        // ── Open New ─────────────────────────────────────────────────────────

        private void OnFrequencySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FrequencyRadioButtons.SelectedItem is RadioButton radioButton)
                viewModel.SelectedFrequency = radioButton.Tag?.ToString() ?? string.Empty;
        }

        private void OnSavingsTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SavingsTypeRadioButtons.SelectedItem is RadioButton radioButton)
            {
                viewModel.SelectedSavingsType = radioButton.Tag?.ToString() ?? string.Empty;

                GoalSavingsPanel.Visibility =
                    viewModel.SelectedSavingsType == "GoalSavings"
                        ? Visibility.Visible : Visibility.Collapsed;

                FixedDepositPanel.Visibility =
                    viewModel.SelectedSavingsType == "FixedDeposit"
                        ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void OnOpenAccountClicked(object sender, RoutedEventArgs e)
        {
            ClearCreateErrors();

            viewModel.AccountName = AccountNameTextBox.Text;
            viewModel.InitialDepositText = InitialDepositTextBox.Text;
            viewModel.SelectedFundingSource =
                FundingSourceComboBox.SelectedItem as KarmaBanking.App.Models.FundingSourceOption;

            if (viewModel.IsGoalSavings)
            {
                if (decimal.TryParse(TargetAmountTextBox.Text, NumberStyles.Any,
                        CultureInfo.InvariantCulture, out decimal targetAmount))
                    viewModel.TargetAmount = targetAmount;
                viewModel.TargetDate = TargetDatePicker.Date;
            }

            if (viewModel.SelectedSavingsType == "FixedDeposit")
            {
                viewModel.MaturityDate = MaturityDatePicker.Date;
            }

            await viewModel.CreateAccountCommand.ExecuteAsync(null);

            if (viewModel.FieldErrors.TryGetValue("SavingsType", out string? savingsTypeError))
                ShowError(TypeErrorText, savingsTypeError);
            if (viewModel.FieldErrors.TryGetValue("AccountName", out string? accountNameError))
                ShowError(AccountNameError, accountNameError);
            if (viewModel.FieldErrors.TryGetValue("InitialDeposit", out string? initialDepositError))
                ShowError(InitialDepositError, initialDepositError);
            if (viewModel.FieldErrors.TryGetValue("FundingSource", out string? fundingSourceError))
                ShowError(FundingSourceError, fundingSourceError);
            if (viewModel.FieldErrors.TryGetValue("Frequency", out string? frequencyError))
                ShowError(FrequencyError, frequencyError);
            if (viewModel.FieldErrors.TryGetValue("TargetAmount", out string? targetAmountError))
                ShowError(TargetAmountError, targetAmountError);
            if (viewModel.FieldErrors.TryGetValue("TargetDate", out string? targetDateError))
                ShowError(TargetDateError, targetDateError);

            if (viewModel.HasError)
            {
                CreateErrorBar.Message = viewModel.ErrorMessage;
                CreateErrorBar.IsOpen = true;
                return;
            }

            if (viewModel.ShowCreateConfirmation)
            {
                CreateSuccessBar.IsOpen = true;
                OpenAccountButton.IsEnabled = false;
                await System.Threading.Tasks.Task.Delay(1500);
                CreateSuccessBar.IsOpen = false;
                OpenAccountButton.IsEnabled = true;
                AccountNameTextBox.Text = string.Empty;
                InitialDepositTextBox.Text = string.Empty;
                SavingsTypeRadioButtons.SelectedIndex = -1;
                MainNavigationView.SelectedItem = MyAccountsTab;
            }
        }

        private void OnCancelCreateClicked(object sender, RoutedEventArgs e)
        {
            MainNavigationView.SelectedItem = MyAccountsTab;
        }

        // ── Manage: account selection ────────────────────────────────────────

        private void OnManageAccountSelected(object sender, SelectionChangedEventArgs e)
        {
            viewModel.SelectedAccount = ManageAccountComboBox.SelectedItem as SavingsAccount;
            HideAllActionPanels();
            ManageButtonsPanel.Visibility = viewModel.SelectedAccount != null
                ? Visibility.Visible : Visibility.Collapsed;
        }

        // ── Manage: show action panels ───────────────────────────────────────

        private async void OnDepositClicked(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedAccount == null) return;

            // Load funding sources into the deposit combobox
            await viewModel.LoadFundingSourcesAsync();
            DepositSourceComboBox.ItemsSource = viewModel.FundingSources;
            if (viewModel.FundingSources.Count > 0)
                DepositSourceComboBox.SelectedIndex = 0;

            // Sync amount field
            DepositAmountTextBox.Text = string.Empty;
            viewModel.DepositAmountText = string.Empty;
            DepositLivePreview.Text = string.Empty;
            DepositResultBar.IsOpen = false;

            HideAllActionPanels();
            ManageButtonsPanel.Visibility = Visibility.Collapsed;
            DepositActionPanel.Visibility = Visibility.Visible;
        }

        private async void OnWithdrawClicked(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedAccount == null) return;

            // Load funding sources as withdraw destinations
            await viewModel.LoadFundingSourcesAsync();
            WithdrawDestComboBox.ItemsSource = viewModel.FundingSources;
            if (viewModel.FundingSources.Count > 0)
            {
                WithdrawDestComboBox.SelectedIndex = 0;
                viewModel.WithdrawDestination = viewModel.FundingSources[0];
            }

            WithdrawAmountTextBox.Text = string.Empty;
            viewModel.WithdrawAmountText = string.Empty;
            WithdrawResultBar.IsOpen = false;

            // Show penalty warning if applicable
            WithdrawPenaltyWarning.Visibility = viewModel.WithdrawHasEarlyRisk
                ? Visibility.Visible : Visibility.Collapsed;
            WithdrawPenaltySummaryText.Text = viewModel.WithdrawPenaltySummary;
            WithdrawPenaltyBreakdown.Visibility = Visibility.Collapsed;

            HideAllActionPanels();
            ManageButtonsPanel.Visibility = Visibility.Collapsed;
            WithdrawActionPanel.Visibility = Visibility.Visible;
        }

        private async void OnAutoDepositClicked(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedAccount == null) return;

            await viewModel.LoadAutoDepositAsync(viewModel.SelectedAccount.Id);

            AutoDepositTitle.Text = viewModel.ExistingLabel + " Auto Deposit";
            AutoDepositAmountTextBox.Text = viewModel.AutoDepositAmountText;

            // Set frequency radio
            AutoDepositFrequencyRadios.SelectedIndex = -1;
            for (int i = 0; i < AutoDepositFrequencyRadios.Items.Count; i++)
            {
                if (AutoDepositFrequencyRadios.Items[i] is RadioButton radioButton &&
                    radioButton.Tag?.ToString() == viewModel.AutoDepositFrequency)
                {
                    AutoDepositFrequencyRadios.SelectedIndex = i;
                    break;
                }
            }

            AutoDepositStartDatePicker.Date = viewModel.AutoDepositStartDate;
            AutoDepositActiveToggle.IsOn = viewModel.AutoDepositIsActive;
            AutoDepositResultBar.IsOpen = false;

            HideAllActionPanels();
            ManageButtonsPanel.Visibility = Visibility.Collapsed;
            AutoDepositActionPanel.Visibility = Visibility.Visible;
        }

        private async void OnCloseAccountClicked(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedAccount == null) return;

            await viewModel.LoadCloseDestinationAccountsAsync();

            CloseDestComboBox.ItemsSource = viewModel.CloseDestinationAccounts;
            CloseResultBar.IsOpen = false;
            CloseConfirmCheckBox.IsChecked = false;
            CloseConfirmButton.IsEnabled = false;

            bool hasNoDest = viewModel.CloseDestinationAccounts.Count == 0;
            CloseNoDestText.Visibility = hasNoDest ? Visibility.Visible : Visibility.Collapsed;
            CloseDestComboBox.Visibility = hasNoDest ? Visibility.Collapsed : Visibility.Visible;

            if (!hasNoDest)
            {
                CloseDestComboBox.SelectedIndex = 0;
            }

            // Show penalty warning for fixed deposit before maturity
            ClosePenaltyWarning.Visibility = viewModel.CloseHasPenalty
                ? Visibility.Visible : Visibility.Collapsed;

            HideAllActionPanels();
            ManageButtonsPanel.Visibility = Visibility.Collapsed;
            CloseAccountActionPanel.Visibility = Visibility.Visible;
        }

        // ── Deposit action ───────────────────────────────────────────────────

        private void OnDepositAmountChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.DepositAmountText = DepositAmountTextBox.Text;
            DepositLivePreview.Text = viewModel.LivePreview;
        }

        private void OnDepositSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepositSourceComboBox.SelectedItem is FundingSourceOption fundingSourceOption)
                viewModel.DepositSource = fundingSourceOption.DisplayName;
        }

        private async void OnDepositConfirmed(object sender, RoutedEventArgs e)
        {
            DepositResultBar.IsOpen = false;
            await viewModel.DepositAsync();

            if (viewModel.HasError)
            {
                DepositResultBar.Severity = InfoBarSeverity.Error;
                DepositResultBar.Message = viewModel.ErrorMessage;
                DepositResultBar.IsOpen = true;
            }
            else if (viewModel.ShowDepositSuccess)
            {
                DepositResultBar.Severity = InfoBarSeverity.Success;
                DepositResultBar.Message = viewModel.DepositSuccessMessage;
                DepositResultBar.IsOpen = true;
                DepositAmountTextBox.Text = string.Empty;
            }
        }

        private void OnDepositBack(object sender, RoutedEventArgs e)
        {
            DepositActionPanel.Visibility = Visibility.Collapsed;
            ManageButtonsPanel.Visibility = Visibility.Visible;
        }

        // ── Withdraw action ──────────────────────────────────────────────────

        private void OnWithdrawAmountChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.WithdrawAmountText = WithdrawAmountTextBox.Text;

            bool hasPenalty = viewModel.WithdrawHasPenalty;
            WithdrawPenaltyBreakdown.Visibility = hasPenalty ? Visibility.Visible : Visibility.Collapsed;
            if (hasPenalty)
            {
                WithdrawPenaltyAmountText.Text = $"Penalty (2%): -${viewModel.WithdrawEstimatedPenalty:N2}";
                WithdrawNetAmountText.Text = $"Net amount received: ${viewModel.WithdrawNetAmount:N2}";
            }
        }

        private void OnWithdrawDestChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.WithdrawDestination = WithdrawDestComboBox.SelectedItem as FundingSourceOption;
        }

        private async void OnWithdrawConfirmed(object sender, RoutedEventArgs e)
        {
            WithdrawResultBar.IsOpen = false;
            bool success = await viewModel.ConfirmWithdrawAsync();

            WithdrawResultBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            WithdrawResultBar.Message = viewModel.WithdrawResultMessage;
            WithdrawResultBar.IsOpen = true;

            if (success)
                WithdrawAmountTextBox.Text = string.Empty;
        }

        private void OnWithdrawBack(object sender, RoutedEventArgs e)
        {
            WithdrawActionPanel.Visibility = Visibility.Collapsed;
            ManageButtonsPanel.Visibility = Visibility.Visible;
        }

        // ── Auto Deposit action ──────────────────────────────────────────────

        private void OnAutoDepositFrequencyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AutoDepositFrequencyRadios.SelectedItem is RadioButton radioButton)
                viewModel.AutoDepositFrequency = radioButton.Tag?.ToString() ?? string.Empty;
        }

        private async void OnAutoDepositSaved(object sender, RoutedEventArgs e)
        {
            AutoDepositResultBar.IsOpen = false;

            viewModel.AutoDepositAmountText = AutoDepositAmountTextBox.Text;
            viewModel.AutoDepositStartDate = AutoDepositStartDatePicker.Date;
            viewModel.AutoDepositIsActive = AutoDepositActiveToggle.IsOn;

            await viewModel.SaveAutoDepositAsync();

            if (viewModel.HasError)
            {
                AutoDepositResultBar.Severity = InfoBarSeverity.Error;
                AutoDepositResultBar.Message = viewModel.ErrorMessage;
                AutoDepositResultBar.IsOpen = true;
            }
            else if (!string.IsNullOrEmpty(viewModel.AutoDepositSaveMessage))
            {
                AutoDepositResultBar.Severity = InfoBarSeverity.Success;
                AutoDepositResultBar.Message = viewModel.AutoDepositSaveMessage;
                AutoDepositResultBar.IsOpen = true;
                AutoDepositTitle.Text = "Modify Auto Deposit";
            }
        }

        private void OnAutoDepositBack(object sender, RoutedEventArgs e)
        {
            AutoDepositActionPanel.Visibility = Visibility.Collapsed;
            ManageButtonsPanel.Visibility = Visibility.Visible;
        }

        // ── Close Account action ─────────────────────────────────────────────

        private void OnCloseDestChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CloseDestComboBox.SelectedItem is SavingsAccount savingsAccount)
                viewModel.SelectedCloseDestinationId = savingsAccount.Id;
        }

        private void OnCloseConfirmChecked(object sender, RoutedEventArgs e)
        {
            viewModel.CloseUserConfirmed = true;
            CloseConfirmButton.IsEnabled = true;
        }

        private void OnCloseConfirmUnchecked(object sender, RoutedEventArgs e)
        {
            viewModel.CloseUserConfirmed = false;
            CloseConfirmButton.IsEnabled = false;
        }

        private async void OnCloseConfirmed(object sender, RoutedEventArgs e)
        {
            CloseResultBar.IsOpen = false;
            bool success = await viewModel.ConfirmCloseAsync();

            CloseResultBar.Severity = success ? InfoBarSeverity.Success : InfoBarSeverity.Error;
            CloseResultBar.Message = viewModel.CloseResultMessage;
            CloseResultBar.IsOpen = true;

            if (success)
            {
                // After successful close, go back to buttons panel after a brief moment
                await System.Threading.Tasks.Task.Delay(1500);
                CloseAccountActionPanel.Visibility = Visibility.Collapsed;
                ManageButtonsPanel.Visibility = Visibility.Visible;
                ManageAccountComboBox.SelectedIndex = -1;
                ManageButtonsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnCloseBack(object sender, RoutedEventArgs e)
        {
            CloseAccountActionPanel.Visibility = Visibility.Collapsed;
            ManageButtonsPanel.Visibility = Visibility.Visible;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void HideAllActionPanels()
        {
            DepositActionPanel.Visibility = Visibility.Collapsed;
            WithdrawActionPanel.Visibility = Visibility.Collapsed;
            AutoDepositActionPanel.Visibility = Visibility.Collapsed;
            CloseAccountActionPanel.Visibility = Visibility.Collapsed;
        }

        private void ClearCreateErrors()
        {
            CreateErrorBar.IsOpen = false;
            CreateSuccessBar.IsOpen = false;
            TypeErrorText.Visibility = Visibility.Collapsed;
            AccountNameError.Visibility = Visibility.Collapsed;
            InitialDepositError.Visibility = Visibility.Collapsed;
            FundingSourceError.Visibility = Visibility.Collapsed;
            TargetAmountError.Visibility = Visibility.Collapsed;
            TargetDateError.Visibility = Visibility.Collapsed;
        }

        private static void ShowError(TextBlock tb, string msg)
        {
            tb.Text = msg;
            tb.Visibility = Visibility.Visible;
        }

        private async System.Threading.Tasks.Task ShowDialogAsync(string title, string msg)
        {
            var contentDialog = new ContentDialog
            {
                Title = title,
                Content = msg,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await contentDialog.ShowAsync();
        }
    }
}
