namespace KarmaBanking.App.Views
{
    using KarmaBanking.App.Services;
    using KarmaBanking.App.ViewModels;
    using Microsoft.UI;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media;
    using Microsoft.UI.Xaml.Navigation;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Windows.Storage;
    using Windows.Storage.FileProperties;
    using Windows.Storage.Pickers;
    using WinRT.Interop;

    public sealed partial class ChatRoutingView : Page
    {
        private readonly ChatViewModel viewModel = ChatViewModel.Instance;
        private int selectedRating = 0;

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
            selectedRating = 0;

            StackPanel dialogContent = new StackPanel { Spacing = 12 };

            TextBlock titleText = new TextBlock
            {
                Text = "Rate your experience",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };

            TextBlock ratingLabel = new TextBlock { Text = "Please select a rating:" };
            StackPanel starsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            TextBlock selectedRatingText = new TextBlock { Text = "No rating selected." };

            for (int i = 1; i <= 5; i++)
            {
                int ratingValue = i;
                Button starButton = new Button { Content = $"⭐{ratingValue}", Tag = ratingValue };
                starButton.Click += (s, args) =>
                {
                    selectedRating = ratingValue;
                    selectedRatingText.Text = $"Selected rating: {ratingValue} ⭐";
                };
                starsPanel.Children.Add(starButton);
            }

            TextBox feedbackTextBox = new TextBox
            {
                PlaceholderText = "Write your feedback here...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100,
            };

            dialogContent.Children.Add(titleText);
            dialogContent.Children.Add(ratingLabel);
            dialogContent.Children.Add(starsPanel);
            dialogContent.Children.Add(selectedRatingText);
            dialogContent.Children.Add(new TextBlock { Text = "Leave feedback (optional):" });
            dialogContent.Children.Add(feedbackTextBox);

            ContentDialog ratingDialog = new ContentDialog
            {
                Title = "Post-Chat Rating",
                PrimaryButtonText = "Submit",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogContent,
                XamlRoot = XamlRoot,
            };

            ContentDialogResult result = await ratingDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (selectedRating == 0)
                {
                    StatusText.Text = "Please select a rating before submitting.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    return;
                }

                try
                {
                    ApiService api = new ApiService();
                    // FIXED: IdentificationNumber
                    int sessionIdentificationNumber = viewModel.CurrentSession?.IdentificationNumber ?? 1;
                    string feedback = feedbackTextBox.Text;

                    api.SubmitFeedback(sessionIdentificationNumber, selectedRating, feedback);

                    StatusText.Text = $"Thank you! Rating submitted: {selectedRating} ⭐";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                catch
                {
                    StatusText.Text = "Feedback submission failed.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
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
            try
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

                BasicProperties properties = await file.GetBasicPropertiesAsync();

                if (properties.Size > 10 * 1024 * 1024)
                {
                    viewModel.StatusMessage = "File size must be 10 MB or less.";
                    viewModel.SetUploadFailed("File size must be 10 MB or less.");
                    return;
                }

                viewModel.SelectedAttachment = new Models.SelectedAttachment
                {
                    FileName = file.Name,
                    FilePath = file.Path,
                    FileType = Path.GetExtension(file.Name).ToLowerInvariant(),
                    FileSizeBytes = (long)properties.Size,
                };

                viewModel.StatusMessage = "Attachment selected successfully.";
                viewModel.SetAttachmentSelected();
                viewModel.SetUploadStarted();
                await Task.Delay(1000);
                viewModel.SetUploadSucceeded();

                AttachmentInfoTextBlock.Text = $"Attached file: {file.Name} ({viewModel.SelectedAttachment.FileSizeDisplay})";
            }
            catch (Exception exception)
            {
                viewModel.StatusMessage = $"Attachment selection failed: {exception.Message}";
                viewModel.SetUploadFailed(exception.Message);
            }
        }
    }
}