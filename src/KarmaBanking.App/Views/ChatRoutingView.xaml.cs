// <copyright file="ChatRoutingView.xaml.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Views;

using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using KarmaBanking.App.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

public sealed partial class ChatRoutingView : Page
{
    private readonly ChatViewModel viewModel = ChatViewModel.Instance;

    public ChatRoutingView()
    {
        this.InitializeComponent();
        this.DataContext = this.viewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        this.SessionTitleTextBlock.Text = this.viewModel.CurrentSession == null
            ? "No active chat selected."
            : $"{this.viewModel.CurrentSession.Title} ({this.viewModel.CurrentSession.SessionModeLabel})";

        this.TranscriptTextBox.Text = this.viewModel.BuildCurrentTranscript();

        this.AttachmentInfoTextBlock.Text = this.viewModel.SelectedAttachment == null
            ? "No file attached."
            : $"Attached file: {this.viewModel.SelectedAttachment.FileName} ({this.viewModel.SelectedAttachment.FileSizeDisplay})";

        if (this.viewModel.CurrentSession != null &&
            !string.IsNullOrWhiteSpace(this.viewModel.CurrentSession.TeamContactMessage))
        {
            this.TeamMessageTextBox.Text = this.viewModel.CurrentSession.TeamContactMessage;
        }
    }

    private async void SendToTeam_Click(object sender, RoutedEventArgs e)
    {
        var wasSent = await this.viewModel.SendCurrentConversationToTeamAsync(this.TeamMessageTextBox.Text);

        if (!wasSent)
        {
            this.StatusText.Text = "The support request could not be sent.";
            this.StatusText.Foreground = new SolidColorBrush(Colors.Red);
            return;
        }

        this.TranscriptTextBox.Text = this.viewModel.BuildCurrentTranscript();
        this.SessionTitleTextBlock.Text = this.viewModel.CurrentSession == null
            ? "No active chat selected."
            : $"{this.viewModel.CurrentSession.Title} ({this.viewModel.CurrentSession.SessionModeLabel})";

        this.StatusText.Text = "The chat session was prepared for the support team.";
        this.StatusText.Foreground = new SolidColorBrush(Colors.Green);
    }

    private async void AttachFileButton_Click(object sender, RoutedEventArgs e)
    {
        await this.PickAttachmentAsync();
    }

    private async void OpenRatingDialog_Click(object sender, RoutedEventArgs e)
    {
        var statusBefore = this.viewModel.StatusMessage;
        await this.viewModel.ShowFeedbackDialogAsync(this.XamlRoot);
        if (this.viewModel.StatusMessage != statusBefore)
        {
            var isSuccess = this.viewModel.StatusMessage.StartsWith("Thank you");
            this.StatusText.Text = this.viewModel.StatusMessage;
            this.StatusText.Foreground = new SolidColorBrush(isSuccess ? Colors.Green : Colors.Red);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.Frame.CanGoBack)
        {
            this.Frame.GoBack();
        }
    }

    private async Task PickAttachmentAsync()
    {
        var picker = new FileOpenPicker();
        var windowHandle = WindowNative.GetWindowHandle(App.MainAppWindow);
        InitializeWithWindow.Initialize(picker, windowHandle);

        picker.ViewMode = PickerViewMode.List;
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".pdf");
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");

        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

        await this.viewModel.ProcessAttachmentAsync(file);

        this.AttachmentInfoTextBlock.Text = this.viewModel.SelectedAttachment == null
            ? "No file attached."
            : $"Attached file: {this.viewModel.SelectedAttachment.FileName} ({this.viewModel.SelectedAttachment.FileSizeDisplay})";
    }
}