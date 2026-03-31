using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

        private void Rating_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string ratingValue)
            {
                selectedRating = int.Parse(ratingValue);
                SelectedRatingText.Text = $"Selected rating: {selectedRating} ⭐";
            }
        }

        private void SubmitRating_Click(object sender, RoutedEventArgs e)
        {
            if (selectedRating == 0)
            {
                StatusText.Text = "Please select a rating first.";
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                return;
            }

            try
            {
                ChatSessionRepository repo = new ChatSessionRepository();

                int sessionId = 1; // temporar

                string feedback = FeedbackTextBox.Text;

                repo.SaveSessionRatingAndFeedback(sessionId, selectedRating, feedback);

                StatusText.Text = $"Rating saved successfully: {selectedRating} ⭐";
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                FeedbackTextBox.Text = "";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error saving rating: {ex.Message}";
                StatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
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