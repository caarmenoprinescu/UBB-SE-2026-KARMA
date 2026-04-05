using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Globalization;
using Microsoft.UI.Text;

namespace KarmaBanking.App.Views
{
    public sealed partial class LoansView : Page
    {
        private LoansViewModel _viewModel;

        public LoansView()
        {
            this.InitializeComponent();

            var repo = new LoanRepository();
            var service = new LoanService(repo);

            _viewModel = new LoansViewModel(service, repo);
            this.DataContext = _viewModel;

            _viewModel.loadLoans();
        }

        private void Schedule_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int loanId = (int)button.Tag;
            var selectedLoan = _viewModel.loans?.FirstOrDefault(currentLoan => currentLoan.id == loanId);

            if (selectedLoan != null)
            {
                Frame.Navigate(typeof(AmortizationScheduleView), selectedLoan);
            }
        }

        private async void Pay_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            int loanId = (int)button.Tag;

            var loan = _viewModel.loans?.FirstOrDefault(currentLoan => currentLoan.id == loanId);
            if (loan == null)
            {
                return;
            }

            decimal selectedAmount = loan.monthlyInstallment;

            StackPanel dialogContent = new StackPanel
            {
                Spacing = 16
            };

            TextBlock accountStatusTitle = new TextBlock
            {
                Text = "Account status",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            Grid accountStatusGrid = new Grid
            {
                ColumnSpacing = 16,
                RowSpacing = 8
            };
            accountStatusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
            accountStatusGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            accountStatusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            accountStatusGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock outstandingBalanceLabel = new TextBlock { Text = "Outstanding balance" };
            TextBlock outstandingBalanceValue = new TextBlock
            {
                Text = loan.outstandingBalance.ToString("C", CultureInfo.CurrentCulture)
            };
            Grid.SetColumn(outstandingBalanceValue, 1);

            TextBlock dueDateLabel = new TextBlock { Text = "Due date" };
            Grid.SetRow(dueDateLabel, 1);
            TextBlock dueDateValue = new TextBlock
            {
                Text = DateTime.Today.ToString("d", CultureInfo.CurrentCulture)
            };
            Grid.SetRow(dueDateValue, 1);
            Grid.SetColumn(dueDateValue, 1);

            accountStatusGrid.Children.Add(outstandingBalanceLabel);
            accountStatusGrid.Children.Add(outstandingBalanceValue);
            accountStatusGrid.Children.Add(dueDateLabel);
            accountStatusGrid.Children.Add(dueDateValue);

            TextBlock sourceAccountTitle = new TextBlock
            {
                Text = "Source account",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            ComboBox sourceAccountComboBox = new ComboBox
            {
                ItemsSource = new[]
                {
                    "Checking Account (**** 1234)",
                    "Savings Account (**** 5678)"
                },
                SelectedIndex = 0
            };

            TextBlock paymentAmountTitle = new TextBlock
            {
                Text = "Payment amount",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            RadioButton minimumInstallmentRadio = new RadioButton
            {
                Content = $"Minimum installment ({loan.monthlyInstallment.ToString("C", CultureInfo.CurrentCulture)})",
                GroupName = "LoanPaymentAmountOption",
                IsChecked = true
            };

            RadioButton customAmountRadio = new RadioButton
            {
                Content = "Custom amount",
                GroupName = "LoanPaymentAmountOption"
            };

            TextBox amountTextBox = new TextBox
            {
                Text = loan.monthlyInstallment.ToString("0.00", CultureInfo.CurrentCulture),
                PlaceholderText = "Enter amount",
                IsEnabled = false
            };

            TextBlock previewTitle = new TextBlock
            {
                Text = "Data after payment",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold
            };

            Grid previewGrid = new Grid
            {
                ColumnSpacing = 16,
                RowSpacing = 8
            };
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            previewGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock balanceAfterLabel = new TextBlock { Text = "- Outstanding balance" };
            TextBlock balanceAfterValue = new TextBlock();
            Grid.SetColumn(balanceAfterValue, 1);

            TextBlock termAfterLabel = new TextBlock { Text = "- Remaining term" };
            Grid.SetRow(termAfterLabel, 1);
            TextBlock termAfterValue = new TextBlock();
            Grid.SetRow(termAfterValue, 1);
            Grid.SetColumn(termAfterValue, 1);

            previewGrid.Children.Add(balanceAfterLabel);
            previewGrid.Children.Add(balanceAfterValue);
            previewGrid.Children.Add(termAfterLabel);
            previewGrid.Children.Add(termAfterValue);

            void UpdatePreview()
            {
                if (minimumInstallmentRadio.IsChecked == true)
                {
                    selectedAmount = loan.monthlyInstallment;
                    amountTextBox.Text = loan.monthlyInstallment.ToString("0.00", CultureInfo.CurrentCulture);
                }
                else if (!decimal.TryParse(amountTextBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out selectedAmount))
                {
                    selectedAmount = 0m;
                }

                decimal balanceAfterPayment = loan.outstandingBalance - selectedAmount;
                if (balanceAfterPayment < 0)
                {
                    balanceAfterPayment = 0;
                }

                balanceAfterValue.Text = balanceAfterPayment.ToString("C", CultureInfo.CurrentCulture);
                int remainingTermPreview = balanceAfterPayment == 0
                    ? 0
                    : Math.Max(0, loan.remainingMonths - 1);
                termAfterValue.Text = $"{remainingTermPreview} mo";
            }

            minimumInstallmentRadio.Checked += (dialogSender, args) =>
            {
                amountTextBox.IsEnabled = false;
                UpdatePreview();
            };

            customAmountRadio.Checked += (dialogSender, args) =>
            {
                amountTextBox.IsEnabled = true;
                amountTextBox.Focus(FocusState.Programmatic);
                amountTextBox.SelectAll();
                UpdatePreview();
            };

            amountTextBox.TextChanged += (dialogSender, args) =>
            {
                if (customAmountRadio.IsChecked == true)
                {
                    UpdatePreview();
                }
            };

            UpdatePreview();

            dialogContent.Children.Add(accountStatusTitle);
            dialogContent.Children.Add(accountStatusGrid);
            dialogContent.Children.Add(sourceAccountTitle);
            dialogContent.Children.Add(sourceAccountComboBox);
            dialogContent.Children.Add(paymentAmountTitle);
            dialogContent.Children.Add(minimumInstallmentRadio);
            dialogContent.Children.Add(customAmountRadio);
            dialogContent.Children.Add(amountTextBox);
            Border previewBorder = new Border
            {
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Child = new StackPanel
                {
                    Spacing = 10,
                    Children =
                    {
                        previewTitle,
                        previewGrid
                    }
                }
            };

            dialogContent.Children.Add(previewBorder);

            ContentDialog paymentDialog = new ContentDialog
            {
                Title = $"Pay installment - {loan.loanType}",
                PrimaryButtonText = "Confirm",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogContent,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await paymentDialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            UpdatePreview();
            try
            {
                _viewModel.makePayment(loanId, selectedAmount);
            }
            catch (ArgumentException ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Payment error",
                    CloseButtonText = "OK",
                    Content = new TextBlock
                    {
                        Text = ex.Message
                    },
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
                return;
            }
            catch (InvalidOperationException ex)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Payment error",
                    CloseButtonText = "OK",
                    Content = new TextBlock
                    {
                        Text = ex.Message
                    },
                    XamlRoot = this.XamlRoot
                };

                await errorDialog.ShowAsync();
                return;
            }

            ContentDialog successDialog = new ContentDialog
            {
                Title = "Payment submitted",
                CloseButtonText = "OK",
                Content = new TextBlock
                {
                    Text = $"Payment request for {selectedAmount.ToString("C", CultureInfo.CurrentCulture)} was submitted successfully."
                },
                XamlRoot = this.XamlRoot
            };

            await successDialog.ShowAsync();
        }

        private void ApplyLoan_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ApplyLoanView));
        }
    }
}
