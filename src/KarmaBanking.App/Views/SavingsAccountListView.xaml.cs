using System;
using System.Threading.Tasks;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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
        }

        private async Task SwitchToMyAccountsTabAsync()
        {
            MainNavigationView.SelectedItem = MyAccountsTab;
            await savingsAccountListViewModel.LoadSavingsAccountsAsync(userId: 1);
        }
    }
}
