using KarmaBanking.App.Repositories;
using KarmaBanking.App.Services;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace KarmaBanking.App.Views
{
    public sealed partial class CreateSavingsAccountView : Page
    {
        private readonly CreateSavingsAccountViewModel createSavingsAccountViewModel;
        private Action? onAccountCreatedCallback;

        public CreateSavingsAccountView()
        {
            InitializeComponent();
            createSavingsAccountViewModel = new CreateSavingsAccountViewModel(
                new SavingsService(new SavingsRepository()));
            DataContext = createSavingsAccountViewModel;
            createSavingsAccountViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);
            if (args.Parameter is Action callback)
                onAccountCreatedCallback = callback;
        }

        private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(createSavingsAccountViewModel.IsAccountCreated)
                && createSavingsAccountViewModel.IsAccountCreated)
            {
                await System.Threading.Tasks.Task.Delay(1500);
                onAccountCreatedCallback?.Invoke();
            }
        }

        private void OnSavingsTypeChecked(object sender, RoutedEventArgs args)
        {
            if (createSavingsAccountViewModel == null) return;
            if (sender is RadioButton checkedRadioButton)
                createSavingsAccountViewModel.SelectedSavingsType = checkedRadioButton.Content.ToString() ?? "Flexible";
        }

        private void OnFundingSourceSelected(object sender, SelectionChangedEventArgs args)
        {
            string? fundingAccountTag = (FundingSourceComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            createSavingsAccountViewModel.SetSelectedFundingAccountFromTag(fundingAccountTag);
        }

        private async void OnOpenAccountClicked(object sender, RoutedEventArgs args)
        {
            string? fundingAccountTag = (FundingSourceComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            createSavingsAccountViewModel.PrepareCreateAccountSubmission(
                AccountNicknameTextBox.Text,
                InitialDepositAmountTextBox.Text,
                fundingAccountTag);

            await createSavingsAccountViewModel.SubmitCreateAccountFormAsync();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs args)
        {
            onAccountCreatedCallback?.Invoke();
        }
    }
}
