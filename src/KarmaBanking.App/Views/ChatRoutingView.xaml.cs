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

namespace KarmaBanking.App.Views
{
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

            StatusText.Text = "The full chat transcript, your note, and the selected attachment details were prepared for the support team.";
            StatusText.Foreground = new SolidColorBrush(Colors.Green);
        }

        private async void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            await PickAttachmentAsync();
            AttachmentInfoTextBlock.Text = viewModel.SelectedAttachment == null
                ? "No file attached."
                : $"Attached file: {viewModel.SelectedAttachment.FileName} ({viewModel.SelectedAttachment.FileSizeDisplay})";
        }

        private async void OpenRatingDialog_Click(object sender, RoutedEventArgs e)
        {
            selectedRating = 0;

            StackPanel dialogContent = new StackPanel
            {
                Spacing = 12
            };

            TextBlock titleText = new TextBlock
            {
                Text = "Rate your experience",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };

            TextBlock ratingLabel = new TextBlock
            {
                Text = "Please select a rating:"
            };

            StackPanel starsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            TextBlock selectedRatingText = new TextBlock
            {
                Text = "No rating selected."
            };

            Button star1 = new Button { Content = "⭐1", Tag = 1 };
            Button star2 = new Button { Content = "⭐2", Tag = 2 };
            Button star3 = new Button { Content = "⭐3", Tag = 3 };
            Button star4 = new Button { Content = "⭐4", Tag = 4 };
            Button star5 = new Button { Content = "⭐5", Tag = 5 };

            star1.Click += (s, args) =>
            {
                selectedRating = 1;
                selectedRatingText.Text = "Selected rating: 1 ⭐";
            };

            star2.Click += (s, args) =>
            {
                selectedRating = 2;
                selectedRatingText.Text = "Selected rating: 2 ⭐";
            };

            star3.Click += (s, args) =>
            {
                selectedRating = 3;
                selectedRatingText.Text = "Selected rating: 3 ⭐";
            };

            star4.Click += (s, args) =>
            {
                selectedRating = 4;
                selectedRatingText.Text = "Selected rating: 4 ⭐";
            };

            star5.Click += (s, args) =>
            {
                selectedRating = 5;
                selectedRatingText.Text = "Selected rating: 5 ⭐";
            };

            starsPanel.Children.Add(star1);
            starsPanel.Children.Add(star2);
            starsPanel.Children.Add(star3);
            starsPanel.Children.Add(star4);
            starsPanel.Children.Add(star5);

            TextBlock feedbackLabel = new TextBlock
            {
                Text = "Leave feedback (optional):"
            };

            TextBox feedbackTextBox = new TextBox
            {
                PlaceholderText = "Write your feedback here...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                Height = 100
            };

            dialogContent.Children.Add(titleText);
            dialogContent.Children.Add(ratingLabel);
            dialogContent.Children.Add(starsPanel);
            dialogContent.Children.Add(selectedRatingText);
            dialogContent.Children.Add(feedbackLabel);
            dialogContent.Children.Add(feedbackTextBox);

            ContentDialog ratingDialog = new ContentDialog
            {
                Title = "Post-Chat Rating",
                PrimaryButtonText = "Submit",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogContent,
                XamlRoot = this.XamlRoot
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

                    int sessionId = viewModel.CurrentSession?.id ?? 1;
                    string feedback = feedbackTextBox.Text;

                    api.SubmitFeedback(sessionId, selectedRating, feedback);

                    StatusText.Text = $"Thank you! Rating submitted: {selectedRating} ⭐";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                }
                catch
                {
                    StatusText.Text = "Feedback submission failed (database connection unavailable locally).";
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

                IntPtr hwnd = WindowNative.GetWindowHandle(App.MainAppWindow);
                InitializeWithWindow.Initialize(picker, hwnd);

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
                    FileSizeBytes = (long)properties.Size
                };

                viewModel.StatusMessage = "Attachment selected successfully.";
                viewModel.SetAttachmentSelected();

                viewModel.SetUploadStarted();
                await Task.Delay(1000);
                viewModel.SetUploadSucceeded();
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = $"Attachment selection failed: {ex.Message}";
                viewModel.SetUploadFailed(ex.Message);
            }
        }
    }
}
