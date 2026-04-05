using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace KarmaBanking.App.Views
{
    public sealed partial class CloseAccountDialog : Page
    {
        private readonly CloseAccountViewModel viewModel;

        public CloseAccountDialog(object parameter)
{
            this.InitializeComponent();

            var account = (SavingsAccount)parameter;

            viewModel = new CloseAccountViewModel(
                new SavingsService(new SavingsRepository()),
                account);

            this.DataContext = viewModel;

            _ = viewModel.LoadAccountsAsync();
}

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            bool success = await viewModel.CloseAsync();

            var dialog = new ContentDialog
            {
                Title = success ? "Success" : "Error",
                Content = success ? "Account closed successfully." : "Please confirm and select destination.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();

            if (success)
                Frame.GoBack();
        }
    }
}