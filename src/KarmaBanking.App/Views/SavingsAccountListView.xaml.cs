using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace KarmaBanking.App.Views
{
    public sealed partial class SavingsAccountListView : Page
    {
        private readonly SavingsAccountListViewModel savingsAccountListViewModel;

        public SavingsAccountListView()
        {
            InitializeComponent();
            savingsAccountListViewModel = new SavingsAccountListViewModel(
                new SavingsService(new SavingsRepository()));
            DataContext = savingsAccountListViewModel;
            MainNavigationView.SelectedItem = MyAccountsTab;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
            await savingsAccountListViewModel.ProcessSchedulesAsync();
            await savingsAccountListViewModel.LoadSavingsAccountsAsync(userId: 1);
            if (!string.IsNullOrEmpty(savingsAccountListViewModel.LoadErrorMessage))
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "karma_error.txt"),
                    $"{DateTime.Now}: {savingsAccountListViewModel.LoadErrorMessage}\n");
        }

        private void OnTabSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem selectedTab) return;

            bool isOpenNew = selectedTab == OpenNewTab;
            bool isManage = selectedTab.Content?.ToString() == "Manage";

            MyAccountsContent.Visibility = isOpenNew ? Visibility.Collapsed : Visibility.Visible;
            OpenNewFrame.Visibility = isOpenNew ? Visibility.Visible : Visibility.Collapsed;
            ManageActionButtons.Visibility = isManage ? Visibility.Visible : Visibility.Collapsed;

            if (isOpenNew)
                OpenNewFrame.Navigate(typeof(CreateSavingsAccountView),
                    new Action(async () => await SwitchToMyAccountsTabAsync()));

            AutoSavePanel.Visibility = isManage ? Visibility.Visible : Visibility.Collapsed;
        }

        private async Task SwitchToMyAccountsTabAsync()
        {
            MainNavigationView.SelectedItem = MyAccountsTab;
            await savingsAccountListViewModel.LoadSavingsAccountsAsync(userId: 1);
        }

        private async void CloseAccount_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListView.SelectedItem is not SavingsAccount selectedAccount)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "No account selected",
                    Content = "Please select an account first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await errorDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "Close Account",
                Content = "Are you sure you want to close this account?\n\n⚠ Early closure may result in penalties.",
                PrimaryButtonText = "Close Account",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var viewModel = (SavingsAccountListViewModel)DataContext;
                await viewModel.CloseSavingsAccountAsync(selectedAccount.Id);
            }
        }

        private async void Deposit_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListView.SelectedItem is not SavingsAccount selectedAccount)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "No account selected",
                    Content = "Please select an account first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await errorDialog.ShowAsync();
                return;
            }

            var amountTextBox = new TextBox
            {
                PlaceholderText = "Enter deposit amount"
            };

            var dialog = new ContentDialog
            {
                Title = "Deposit Funds",
                Content = amountTextBox,
                PrimaryButtonText = "Deposit",
                CloseButtonText = "Cancel",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (!decimal.TryParse(amountTextBox.Text, out decimal amount) || amount <= 0)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Invalid amount",
                        Content = "Please enter a valid positive number.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot
                    };

                    await errorDialog.ShowAsync();
                    return;
                }

                var viewModel = (SavingsAccountListViewModel)DataContext;
                await viewModel.DepositAsync(selectedAccount.Id, amount);
            }
        }

        private async void SaveAutoSave_Click(object sender, RoutedEventArgs e)
        {
            if (AccountsListView.SelectedItem is not SavingsAccount selectedAccount)
            {
                var dialog = new ContentDialog
                {
                    Title = "No account selected",
                    Content = "Please select an account first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
                return;
            }

            if (!decimal.TryParse(AutoSaveAmountTextBox.Text, out decimal amount))
            {
                return;
            }

            string frequency = (FrequencyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var viewModel = (SavingsAccountListViewModel)DataContext;

            await viewModel.CreateScheduleAsync(selectedAccount.Id, amount, frequency);
        }
    }
}
