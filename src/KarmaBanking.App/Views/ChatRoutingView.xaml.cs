namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media;
    using Microsoft.UI.Xaml.Navigation;
    using System;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using WinRT.Interop;

    public sealed partial class ChatRoutingView : Page
    {
        private readonly ChatViewModel viewModel = ChatViewModel.Instance;

        public ChatRoutingView()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SessionTitleTextBlock.Text = viewModel.CurrentSession == null
                ? "No active chat selected."
                : $"{viewModel.CurrentSession.Title} ({viewModel.CurrentSession.SessionModeLabel})";

            TranscriptTextBox.Text = viewModel.BuildCurrentTranscript();

            AttachmentInfoTextBlock.Text = viewModel.SelectedAttachment == null
                ? "No file attached."
                : $"Attached file: {viewModel.SelectedAttachment.FileName} ({viewModel.SelectedAttachment.FileSizeDisplay})";

            if (viewModel.CurrentSession != null && !string.IsNullOrWhiteSpace(viewModel.CurrentSession.TeamContactMessage))
            {
                TeamMessageTextBox.Text = viewModel.CurrentSession.TeamContactMessage;
            }
        }

        private async void SendToTeam_Click(object sender, RoutedEventArgs e)
        {
            bool wasSent = await viewModel.SendCurrentConversationToTeamAsync(TeamMessageTextBox.Text);

            if (!wasSent)
            {
                StatusText.Text = "The support request could not be sent.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            TranscriptTextBox.Text = viewModel.BuildCurrentTranscript();
            SessionTitleTextBlock.Text = viewModel.CurrentSession == null
                ? "No active chat selected."
                : $"{viewModel.CurrentSession.Title} ({viewModel.CurrentSession.SessionModeLabel})";

            StatusText.Text = "The chat session was prepared for the support team.";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }

        private async void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            await PickAttachmentAsync();
        }

        private async void OpenRatingDialog_Click(object sender, RoutedEventArgs e)
        {
            string statusBefore = viewModel.StatusMessage;
            await viewModel.ShowFeedbackDialogAsync(XamlRoot);
            if (viewModel.StatusMessage != statusBefore)
            {
                bool isSuccess = viewModel.StatusMessage.StartsWith("Thank you");
                StatusText.Text = viewModel.StatusMessage;
                StatusText.Foreground = new SolidColorBrush(isSuccess ? Colors.Green : Colors.Red);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async Task PickAttachmentAsync()
        {
            FileOpenPicker picker = new FileOpenPicker();
            IntPtr windowHandle = WindowNative.GetWindowHandle(App.MainAppWindow);
            InitializeWithWindow.Initialize(picker, windowHandle);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            await viewModel.ProcessAttachmentAsync(file);

            AttachmentInfoTextBlock.Text = viewModel.SelectedAttachment == null
                ? "No file attached."
                : $"Attached file: {viewModel.SelectedAttachment.FileName} ({viewModel.SelectedAttachment.FileSizeDisplay})";
        }
    }
}