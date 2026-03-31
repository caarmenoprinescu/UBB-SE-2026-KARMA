using KarmaBanking.App.Models;
using KarmaBanking.App.ViewModels;
using Microsoft.UI.Xaml.Controls;
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
    public sealed partial class ChatView : Page
    {
        public ChatViewModel ViewModel { get; }

        public ChatView()
        {
            InitializeComponent();
            ViewModel = ChatViewModel.Instance;
            ViewModel.ContinueRequested += OnContinueRequested;
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await ViewModel.LoadChatHistoryAsync(1);
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

        private void OnContinueRequested(string selectedCategory)
        {
            Frame.Navigate(typeof(ChatRoutingView), selectedCategory);
        }

        private async void AttachFileButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            await PickAttachmentAsync();
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
                    return;

                BasicProperties properties = await file.GetBasicPropertiesAsync();

                if (properties.Size > 10 * 1024 * 1024)
                {
                    ViewModel.StatusMessage = "File size must be 10 MB or less.";
                    ViewModel.SetUploadFailed("File size must be 10 MB or less.");
                    return;
                }

                ViewModel.SelectedAttachment = new SelectedAttachment
                {
                    FileName = file.Name,
                    FilePath = file.Path,
                    FileType = Path.GetExtension(file.Name).ToLowerInvariant(),
                    FileSizeBytes = (long)properties.Size
                };

                ViewModel.StatusMessage = "Attachment selected successfully.";
                ViewModel.SetAttachmentSelected();

                ViewModel.SetUploadStarted();
                await Task.Delay(1000);
                ViewModel.SetUploadSucceeded();
            }
            catch (Exception ex)
            {
                ViewModel.StatusMessage = $"Attachment selection failed: {ex.Message}";
                ViewModel.SetUploadFailed(ex.Message);
            }
        }
    }
}