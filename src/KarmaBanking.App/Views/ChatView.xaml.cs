using KarmaBanking.App.Models;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace KarmaBanking.App.Views
{
    public sealed partial class ChatView : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatView()
        {
            InitializeComponent();
            ViewModel = ChatViewModel.Instance;
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadSessionsAsync();
            SessionsListView.SelectedItem = ViewModel.CurrentSession;
        }

        private async void PresetQuestionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is string question)
            {
                await ViewModel.AskPresetQuestionAsync(question);
                SessionsListView.SelectedItem = ViewModel.CurrentSession;
            }
        }

        private async void SessionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsListView.SelectedItem is ChatSession session)
            {
                await ViewModel.SelectSessionAsync(session);
            }
        }

        private void ContactTeamButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ChatRoutingView));
        }
    }
}