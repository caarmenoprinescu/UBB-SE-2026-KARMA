using KarmaBanking.App.Models;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace KarmaBanking.App.Views
{
    public sealed partial class ChatView : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatView()
        {
            InitializeComponent();
            ViewModel = new ChatViewModel();
            DataContext = ViewModel;
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is string selectedCategory)
            {
                ViewModel.SelectedCategory = selectedCategory switch
                {
                    "Account" => IssueCategory.Account,
                    "Cards" => IssueCategory.Cards,
                    "Transfers" => IssueCategory.Transfers,
                    "Loans" => IssueCategory.Loans,
                    "Technical Issue" => IssueCategory.TechnicalIssue,
                    "Other" => IssueCategory.Other,
                    _ => null
                };
            }
        }
    }
}