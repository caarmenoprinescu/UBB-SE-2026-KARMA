using System;
using System.Globalization;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
            OpenNewPanel.Visibility    = tag == "OpenNew"    ? Visibility.Visible : Visibility.Collapsed;
            ManagePanel.Visibility     = tag == "Manage"     ? Visibility.Visible : Visibility.Collapsed;

            if (tag == "OpenNew")
            {
                SavingsTypeRadioButtons.SelectedIndex = -1;
                viewModel.SelectedSavingsType = string.Empty;
                GoalSavingsPanel.Visibility = Visibility.Collapsed;
                ClearCreateErrors();

                await viewModel.LoadFundingSourcesAsync();
                FundingSourceComboBox.ItemsSource = viewModel.FundingSources;
                if (viewModel.FundingSources.Count > 0)
                    FundingSourceComboBox.SelectedIndex = 0;
            }
        }

        // ── Open New ─────────────────────────────────────────────────────────

        private void OnSavingsTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SavingsTypeRadioButtons.SelectedItem is RadioButton rb)
            {
                viewModel.SelectedSavingsType = rb.Tag?.ToString() ?? string.Empty;
                GoalSavingsPanel.Visibility = viewModel.IsGoalSavings
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
                        CultureInfo.InvariantCulture, out decimal ta))
                    viewModel.TargetAmount = ta;
                viewModel.TargetDate = TargetDatePicker.Date;
            }

            await viewModel.CreateAccountCommand.ExecuteAsync(null);

            if (viewModel.FieldErrors.TryGetValue("SavingsType", out string? te))
                ShowError(TypeErrorText, te);
            if (viewModel.FieldErrors.TryGetValue("AccountName", out string? ne))
                ShowError(AccountNameError, ne);
            if (viewModel.FieldErrors.TryGetValue("InitialDeposit", out string? de))
                ShowError(InitialDepositError, de);
            if (viewModel.FieldErrors.TryGetValue("FundingSource", out string? fe))
                ShowError(FundingSourceError, fe);
            if (viewModel.FieldErrors.TryGetValue("TargetAmount", out string? tae))
                ShowError(TargetAmountError, tae);
            if (viewModel.FieldErrors.TryGetValue("TargetDate", out string? tde))
                ShowError(TargetDateError, tde);

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

        // ── Manage ───────────────────────────────────────────────────────────

        private void OnManageAccountSelected(object sender, SelectionChangedEventArgs e)
        {
            viewModel.SelectedAccount = ManageAccountComboBox.SelectedItem as SavingsAccount;
            DepositSection.Visibility = viewModel.SelectedAccount != null
                ? Visibility.Visible : Visibility.Collapsed;
            DepositAmountTextBox.Text = string.Empty;
            DepositPreviewText.Text = string.Empty;
            DepositSuccessBar.IsOpen = false;
            DepositErrorBar.IsOpen = false;
        }

        private void OnDepositAmountChanged(object sender, TextChangedEventArgs e)
        {
            viewModel.DepositAmountText = DepositAmountTextBox.Text;
            DepositPreviewText.Text = viewModel.LivePreview;
        }

        private async void OnDepositClicked(object sender, RoutedEventArgs e)
        {
            DepositErrorBar.IsOpen = false;
            DepositSuccessBar.IsOpen = false;

            viewModel.DepositAmountText = DepositAmountTextBox.Text;
            viewModel.DepositSource = DepositSourceTextBox.Text;

            await viewModel.DepositCommand.ExecuteAsync(null);

            if (viewModel.HasError)
            {
                DepositErrorBar.Message = viewModel.ErrorMessage;
                DepositErrorBar.IsOpen = true;
            }
            else if (viewModel.ShowDepositSuccess)
            {
                DepositSuccessBar.Message = viewModel.DepositSuccessMessage;
                DepositSuccessBar.IsOpen = true;
                DepositAmountTextBox.Text = string.Empty;
                DepositPreviewText.Text = string.Empty;
            }
        }

        private async void OnCloseAccountClicked(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedAccount == null) return;

            var dialog = new ContentDialog
            {
                Title = "Close Account",
                Content = $"Are you sure you want to close \"{viewModel.SelectedAccount.AccountName}\"?",
                PrimaryButtonText = "Close Account",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                await viewModel.CloseAccountCommand.ExecuteAsync(viewModel.SelectedAccount);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
            var d = new ContentDialog
            {
                Title = title, Content = msg,
                CloseButtonText = "OK", XamlRoot = this.XamlRoot
            };
            await d.ShowAsync();
        }
    }
}
