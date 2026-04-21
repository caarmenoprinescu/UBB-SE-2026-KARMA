// <copyright file="ChatViewModel.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.ViewModels;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;
using KarmaBanking.App.Models;
using KarmaBanking.App.Services;
using KarmaBanking.App.Utils;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public class ChatViewModel : INotifyPropertyChanged
{
    private readonly ApiService apiService = new MockApiService();
    private readonly ChatCategoryService chatCategoryService = new();
    private readonly ChatSessionService chatSessionService = new();
    private readonly DialogService dialogService = new();
    private readonly FileValidationService fileValidationService = new();
    private ObservableCollection<ChatMessage> chatMessages = [];
    private ObservableCollection<ChatSession> chatSessions = [];
    private ChatSession? currentSession;
    private bool isUploading;
    private int nextSessionIdentificationNumber = 1;
    private ObservableCollection<string> presetQuestions = [];
    private SelectedAttachment? selectedAttachment;
    private string statusMessage = "Choose a preset question to start a chat session.";
    private string uploadStatusMessage = "No file uploaded.";

    private ChatViewModel()
    {
        this.StartNewSessionCommand = new RelayCommand(this.OnStartNewSessionAsync);
        this.RemoveAttachmentCommand = new RelayCommand(this.OnRemoveAttachmentAsync, () => this.CanRemoveAttachment);
        this.CreateSession();
        _ = this.LoadPresetQuestionsAsync();
    }

    public static ChatViewModel Instance { get; } = new();

    public ObservableCollection<ChatSession> Sessions
    {
        get => this.chatSessions;
        set
        {
            if (this.chatSessions != value)
            {
                this.chatSessions = value;
                this.OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ChatMessage> Messages
    {
        get => this.chatMessages;
        set
        {
            if (this.chatMessages != value)
            {
                this.chatMessages = value;
                this.OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<string> PresetQuestions
    {
        get => this.presetQuestions;
        set
        {
            if (this.presetQuestions != value)
            {
                this.presetQuestions = value;
                this.OnPropertyChanged();
            }
        }
    }

    public ChatSession? CurrentSession
    {
        get => this.currentSession;
        set
        {
            if (this.currentSession != value)
            {
                this.currentSession = value;
                this.Messages = this.currentSession?.Messages ?? [];
                this.selectedAttachment = this.currentSession?.Attachment;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.SelectedAttachment));
                this.OnPropertyChanged(nameof(this.CurrentSessionTitle));
                this.OnPropertyChanged(nameof(this.CurrentSessionModeLabel));
                this.OnPropertyChanged(nameof(this.CanContactTeam));
                this.OnPropertyChanged(nameof(this.HasMessages));
                this.OnPropertyChanged(nameof(this.HasAttachmentPreview));
                this.OnPropertyChanged(nameof(this.CanAttachFile));
                this.OnPropertyChanged(nameof(this.CanRemoveAttachment));
                this.RemoveAttachmentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentSessionTitle => this.CurrentSession?.Title ?? "Customer Support";

    public string CurrentSessionModeLabel => this.CurrentSession?.SessionModeLabel ?? "Chatbot assistance";

    public bool HasMessages => this.Messages.Count > 0;

    public SelectedAttachment? SelectedAttachment
    {
        get => this.selectedAttachment;
        set
        {
            if (this.selectedAttachment != value)
            {
                this.selectedAttachment = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.HasAttachmentPreview));
                if (this.CurrentSession != null)
                {
                    this.CurrentSession.Attachment = value;
                    this.UpdateSessionSummary(this.CurrentSession);
                }

                this.OnPropertyChanged(nameof(this.CanRemoveAttachment));
            }
        }
    }

    public bool HasAttachmentPreview => this.SelectedAttachment != null;

    public bool IsUploading
    {
        get => this.isUploading;
        set
        {
            if (this.isUploading != value)
            {
                this.isUploading = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.CanAttachFile));
                this.OnPropertyChanged(nameof(this.CanRemoveAttachment));
                this.RemoveAttachmentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool CanAttachFile => this.CurrentSession != null && !this.IsUploading;

    public bool CanRemoveAttachment => this.SelectedAttachment != null && !this.IsUploading;

    public bool CanContactTeam => this.CurrentSession != null;

    public string UploadStatusMessage
    {
        get => this.uploadStatusMessage;
        set
        {
            if (this.uploadStatusMessage != value)
            {
                this.uploadStatusMessage = value;
                this.OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => this.statusMessage;
        set
        {
            if (this.statusMessage != value)
            {
                this.statusMessage = value;
                this.OnPropertyChanged();
            }
        }
    }

    public RelayCommand StartNewSessionCommand { get; }

    public RelayCommand RemoveAttachmentCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    // --- THESE ARE THE MISSING METHODS ---
    public void SetUploadStarted()
    {
        this.IsUploading = true;
        this.UploadStatusMessage = "Uploading attachment...";
    }

    public void SetUploadSucceeded()
    {
        this.IsUploading = false;
        this.UploadStatusMessage = "Attachment uploaded successfully.";
    }

    public void SetUploadFailed(string errorMessage)
    {
        this.IsUploading = false;
        this.UploadStatusMessage = $"Upload failed: {errorMessage}";
    }

    public void SetAttachmentSelected()
    {
        this.UploadStatusMessage = "Attachment selected. Ready to upload.";
    }

    public void SelectSession(ChatSession? session)
    {
        if (session == null)
        {
            return;
        }

        this.CurrentSession = session;
        this.StatusMessage = $"Viewing {session.Title.ToLowerInvariant()}.";
    }

    public async Task AskPresetQuestionAsync(string question)
    {
        var response = await this.apiService.GetChatbotPresetAnswerAsync(question);

        if (string.IsNullOrWhiteSpace(response))
        {
            return;
        }

        this.EnsureSession();
        var session = this.CurrentSession!;
        session.IssueCategory = this.chatCategoryService.InferCategory(question);

        session.Messages.Add(
            this.chatSessionService.CreateMessage(session, session.Messages.Count + 1, "USER", question, DateTime.Now));
        session.Messages.Add(
            this.chatSessionService.CreateMessage(
                session,
                session.Messages.Count + 1,
                "CHATBOT ASSISTANCE",
                response,
                DateTime.Now.AddSeconds(1)));

        this.UpdateSessionSummary(session, question, response);
        this.StatusMessage = "Preset answer added to the chat.";
        this.OnPropertyChanged(nameof(this.HasMessages));
    }

    public async Task<bool> SendCurrentConversationToTeamAsync(string customerMessage)
    {
        if (this.CurrentSession == null)
        {
            return false;
        }

        var trimmedMessage = customerMessage?.Trim() ?? string.Empty;
        var transcript = this.BuildCurrentTranscript();
        var wasSent = await this.apiService.SendChatToSupportAsync(transcript, trimmedMessage, this.SelectedAttachment);

        if (!wasSent)
        {
            this.StatusMessage = "The chat could not be sent to the team.";
            return false;
        }

        this.CurrentSession.TeamContactMessage = trimmedMessage;
        this.CurrentSession.IsEscalatedToTeam = true;
        this.CurrentSession.SessionStatus = "Escalated";

        this.CurrentSession.Messages.Add(
            this.chatSessionService.CreateMessage(
                this.CurrentSession,
                this.CurrentSession.Messages.Count + 1,
                "SYSTEM",
                "Conversation sent to the Karma Banking team.",
                DateTime.Now));

        this.UpdateSessionSummary(this.CurrentSession);
        this.StatusMessage = "The current chat session was sent to the team.";
        this.OnPropertyChanged(nameof(this.CurrentSessionModeLabel));
        return true;
    }

    public string BuildCurrentTranscript()
    {
        return this.chatSessionService.BuildTranscript(this.CurrentSession);
    }

    /// <summary>
    ///     Processes a selected file for attachment, validating it before adding to the session.
    /// </summary>
    public async Task ProcessAttachmentAsync(StorageFile file)
    {
        try
        {
            // Validate file
            var (isValid, errorMessage) = await this.fileValidationService.ValidateFileAsync(file);

            if (!isValid)
            {
                this.StatusMessage = errorMessage;
                this.SetUploadFailed(errorMessage);
                return;
            }

            // Map file to attachment model
            this.SelectedAttachment = await this.fileValidationService.MapStorageFileToAttachmentAsync(file);

            this.StatusMessage = "Attachment selected successfully.";
            this.SetAttachmentSelected();
            this.SetUploadStarted();
            await Task.Delay(1000);
            this.SetUploadSucceeded();
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Attachment processing failed: {ex.Message}";
            this.SetUploadFailed(ex.Message);
        }
    }

    /// <summary>
    ///     Shows a feedback dialog for rating the chat experience.
    /// </summary>
    public async Task ShowFeedbackDialogAsync(XamlRoot xamlRoot)
    {
        if (this.CurrentSession == null)
        {
            return;
        }

        try
        {
            // Create dialog content dynamically
            var dialogContent = new StackPanel { Spacing = 12 };

            var titleText = new TextBlock
            {
                Text = "Rate your experience",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
            };

            var ratingLabel = new TextBlock { Text = "Please select a rating:" };
            var starsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
            };
            var selectedRatingText = new TextBlock { Text = "No rating selected." };
            var selectedRating = 0;

            for (var i = 1; i <= 5; i++)
            {
                var ratingValue = i;
                var starButton = new Button
                {
                    Content = $"⭐{ratingValue}",
                    Tag = ratingValue,
                };
                starButton.Click += (s, args) =>
                {
                    selectedRating = ratingValue;
                    selectedRatingText.Text = $"Selected rating: {ratingValue} ⭐";
                };
                starsPanel.Children.Add(starButton);
            }

            var feedbackTextBox = new TextBox
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

            var ratingDialog = new ContentDialog
            {
                Title = "Post-Chat Rating",
                PrimaryButtonText = "Submit",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogContent,
                XamlRoot = xamlRoot,
            };

            var result = await ratingDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (selectedRating == 0)
                {
                    this.StatusMessage = "Please select a rating before submitting.";
                    return;
                }

                // Submit feedback through API
                var sessionIdentificationNumber = this.CurrentSession?.IdentificationNumber ?? 1;
                var feedback = feedbackTextBox.Text;

                await this.SubmitFeedbackAsync(sessionIdentificationNumber, selectedRating, feedback);
            }
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Feedback dialog failed: {ex.Message}";
        }
    }

    /// <summary>
    ///     Submits feedback to the API.
    /// </summary>
    private async Task SubmitFeedbackAsync(int sessionId, int rating, string feedback)
    {
        try
        {
            this.apiService.SubmitFeedback(sessionId, rating, feedback);
            this.StatusMessage = $"Thank you! Rating submitted: {rating} ⭐";
        }
        catch (Exception ex)
        {
            this.StatusMessage = $"Feedback submission failed: {ex.Message}";
        }
    }

    public void EnsureSession()
    {
        if (this.CurrentSession == null)
        {
            this.CreateSession();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private Task OnStartNewSessionAsync()
    {
        this.CreateSession();
        return Task.CompletedTask;
    }

    private Task OnRemoveAttachmentAsync()
    {
        if (!this.CanRemoveAttachment)
        {
            return Task.CompletedTask;
        }

        this.SelectedAttachment = null;
        this.UploadStatusMessage = "Attachment removed.";
        this.StatusMessage = "Attachment removed from the current chat session.";

        return Task.CompletedTask;
    }

    private async Task LoadPresetQuestionsAsync()
    {
        var questions = await this.apiService.GetChatbotPresetQuestionsAsync();
        if (questions.Count > 0)
        {
            this.PresetQuestions = new ObservableCollection<string>(questions);
        }
    }

    private void CreateSession()
    {
        var session = this.chatSessionService.CreateSession(this.nextSessionIdentificationNumber++);
        this.Sessions.Insert(0, session);
        this.CurrentSession = session;
    }

    private void UpdateSessionSummary(ChatSession session, string? selectedQuestion = null, string? response = null)
    {
        session.LastUpdatedAt = DateTime.Now;
    }
}