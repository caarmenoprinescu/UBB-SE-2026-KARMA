using KarmaBanking.App.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace KarmaBanking.App.Views
{
    public sealed partial class ChatRoutingView : Page
    {
        private int selectedRating = 0;

        public ChatRoutingView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string category)
            {
                CategoryTextBlock.Text = $"Category: {category}";
            }
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

                    int sessionId = 1; // temporar
                    string feedback = feedbackTextBox.Text;

                    api.SubmitFeedback(sessionId, selectedRating, feedback);
                    api.EmailSessionTranscript(sessionId, "client@example.com"); //temporar

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
    }
}