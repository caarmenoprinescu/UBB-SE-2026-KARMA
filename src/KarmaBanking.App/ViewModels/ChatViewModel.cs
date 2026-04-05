using KarmaBanking.App.Models;
using KarmaBanking.App.Services;
using KarmaBanking.App.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace KarmaBanking.App.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        public static ChatViewModel Instance { get; } = new ChatViewModel();

        private readonly ApiService apiService = new MockApiService();
        private ObservableCollection<ChatSession> sessions = new ObservableCollection<ChatSession>();
        private ObservableCollection<ChatMessage> messages = new ObservableCollection<ChatMessage>();
        private ObservableCollection<string> presetQuestions = new ObservableCollection<string>();
        private ChatSession? currentSession;
        private string statusMessage = "Choose a preset question to start a chat session.";
        private SelectedAttachment? selectedAttachment;
        private bool isUploading;
        private string uploadStatusMessage = "No file uploaded.";
        private int nextSessionId = 1;

        public ObservableCollection<ChatSession> Sessions
        {
            get => sessions;
            set
            {
                if (sessions != value)
                {
                    sessions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get => messages;
            set
            {
                if (messages != value)
                {
                    messages = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> PresetQuestions
        {
            get => presetQuestions;
            set
            {
                if (presetQuestions != value)
                {
                    presetQuestions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ChatSession? CurrentSession
        {
            get => currentSession;
            set
            {
                if (currentSession != value)
                {
                    currentSession = value;
                    Messages = currentSession?.Messages ?? new ObservableCollection<ChatMessage>();
                    selectedAttachment = currentSession?.Attachment;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedAttachment));
                    OnPropertyChanged(nameof(CurrentSessionTitle));
                    OnPropertyChanged(nameof(CurrentSessionModeLabel));
                    OnPropertyChanged(nameof(CanContactTeam));
                    OnPropertyChanged(nameof(HasMessages));
                    OnPropertyChanged(nameof(HasAttachmentPreview));
                    OnPropertyChanged(nameof(CanAttachFile));
                    OnPropertyChanged(nameof(CanRemoveAttachment));
                    RemoveAttachmentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string CurrentSessionTitle => CurrentSession?.Title ?? "Customer Support";

        public string CurrentSessionModeLabel => CurrentSession?.SessionModeLabel ?? "Chatbot assistance";

        public bool HasMessages => Messages.Count > 0;

        public SelectedAttachment? SelectedAttachment
        {
            get => selectedAttachment;
            set
            {
                if (selectedAttachment != value)
                {
                    selectedAttachment = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasAttachmentPreview));
                    if (CurrentSession != null)
                    {
                        CurrentSession.Attachment = value;
                        UpdateSessionSummary(CurrentSession);
                    }

                    OnPropertyChanged(nameof(CanRemoveAttachment));
                }
            }
        }

        public bool HasAttachmentPreview => SelectedAttachment != null;

        public bool IsUploading
        {
            get => isUploading;
            set
            {
                if (isUploading != value)
                {
                    isUploading = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAttachFile));
                    OnPropertyChanged(nameof(CanRemoveAttachment));
                    RemoveAttachmentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanAttachFile => CurrentSession != null && !IsUploading;

        public bool CanRemoveAttachment => SelectedAttachment != null && !IsUploading;

        public bool CanContactTeam => CurrentSession != null;

        public string UploadStatusMessage
        {
            get => uploadStatusMessage;
            set
            {
                if (uploadStatusMessage != value)
                {
                    uploadStatusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => statusMessage;
            set
            {
                if (statusMessage != value)
                {
                    statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand StartNewSessionCommand { get; }
        public RelayCommand RemoveAttachmentCommand { get; }

        private ChatViewModel()
        {
            StartNewSessionCommand = new RelayCommand(OnStartNewSessionAsync);
            RemoveAttachmentCommand = new RelayCommand(OnRemoveAttachmentAsync, () => CanRemoveAttachment);
            CreateSession();
            _ = LoadPresetQuestionsAsync();
        }

        private Task OnStartNewSessionAsync()
        {
            CreateSession();
            return Task.CompletedTask;
        }

        private Task OnRemoveAttachmentAsync()
        {
            if (!CanRemoveAttachment)
                return Task.CompletedTask;

            SelectedAttachment = null;
            UploadStatusMessage = "Attachment removed.";
            StatusMessage = "Attachment removed from the current chat session.";

            return Task.CompletedTask;
        }

        public void SetUploadStarted()
        {
            IsUploading = true;
            UploadStatusMessage = "Uploading attachment...";
        }

        public void SetUploadSucceeded()
        {
            IsUploading = false;
            UploadStatusMessage = "Attachment uploaded successfully.";
        }

        public void SetUploadFailed(string errorMessage)
        {
            IsUploading = false;
            UploadStatusMessage = $"Upload failed: {errorMessage}";
        }

        public void SetAttachmentSelected()
        {
            UploadStatusMessage = "Attachment selected. Ready to upload.";
        }

        public void SelectSession(ChatSession? session)
        {
            if (session == null)
            {
                return;
            }

            CurrentSession = session;
            StatusMessage = $"Viewing {session.Title.ToLowerInvariant()}.";
        }

        public async Task AskPresetQuestionAsync(string question)
        {
            string response = await apiService.GetChatbotPresetAnswerAsync(question);

            if (string.IsNullOrWhiteSpace(response))
            {
                return;
            }

            if (CurrentSession == null)
            {
                CreateSession();
            }

            ChatSession session = CurrentSession!;
            session.issueCategory = InferCategory(question);

            session.Messages.Add(new ChatMessage
            {
                Id = session.Messages.Count + 1,
                SessionId = session.id,
                SenderType = "USER",
                Content = question,
                SentAt = DateTime.Now
            });

            session.Messages.Add(new ChatMessage
            {
                Id = session.Messages.Count + 1,
                SessionId = session.id,
                SenderType = "CHATBOT ASSISTANCE",
                Content = response,
                SentAt = DateTime.Now.AddSeconds(1)
            });

            UpdateSessionSummary(session, question, response);
            StatusMessage = "Preset answer added to the chat.";
            OnPropertyChanged(nameof(HasMessages));
        }

        public async Task<bool> SendCurrentConversationToTeamAsync(string customerMessage)
        {
            if (CurrentSession == null)
            {
                return false;
            }

            string trimmedMessage = customerMessage?.Trim() ?? string.Empty;
            string transcript = BuildCurrentTranscript();
            bool wasSent = await apiService.SendChatToSupportAsync(transcript, trimmedMessage, SelectedAttachment);

            if (!wasSent)
            {
                StatusMessage = "The chat could not be sent to the team.";
                return false;
            }

            CurrentSession.TeamContactMessage = trimmedMessage;
            CurrentSession.IsEscalatedToTeam = true;
            CurrentSession.sessionStatus = "Escalated";

            string attachmentMessage = SelectedAttachment == null
                ? "No attachment was added."
                : $"Attached file: {SelectedAttachment.FileName} ({SelectedAttachment.FileSizeDisplay}).";

            CurrentSession.Messages.Add(new ChatMessage
            {
                Id = CurrentSession.Messages.Count + 1,
                SessionId = CurrentSession.id,
                SenderType = "SYSTEM",
                Content = $"Conversation sent to the Karma Banking team. {attachmentMessage}",
                SentAt = DateTime.Now
            });

            if (!string.IsNullOrWhiteSpace(trimmedMessage))
            {
                CurrentSession.Messages.Add(new ChatMessage
                {
                    Id = CurrentSession.Messages.Count + 1,
                    SessionId = CurrentSession.id,
                    SenderType = "CUSTOMER NOTE",
                    Content = trimmedMessage,
                    SentAt = DateTime.Now.AddSeconds(1)
                });
            }

            UpdateSessionSummary(CurrentSession);
            StatusMessage = "The current chat session was sent to the team.";
            OnPropertyChanged(nameof(CurrentSessionModeLabel));
            return true;
        }

        public string BuildCurrentTranscript()
        {
            if (CurrentSession == null)
            {
                return "No chat session selected.";
            }

            List<string> lines = CurrentSession.Messages
                .Select(message => $"[{message.SentAt:g}] {message.SenderType}: {message.Content}")
                .ToList();

            if (!string.IsNullOrWhiteSpace(CurrentSession.TeamContactMessage))
            {
                lines.Add(string.Empty);
                lines.Add($"Customer note: {CurrentSession.TeamContactMessage}");
            }

            if (CurrentSession.Attachment != null)
            {
                lines.Add($"Attachment: {CurrentSession.Attachment.FileName} ({CurrentSession.Attachment.FileSizeDisplay})");
            }

            return string.Join(Environment.NewLine, lines);
        }

        public void EnsureSession()
        {
            if (CurrentSession == null)
            {
                CreateSession();
            }
        }

        private async Task LoadPresetQuestionsAsync()
        {
            List<string> questions = await apiService.GetChatbotPresetQuestionsAsync();

            if (questions.Count == 0)
            {
                return;
            }

            PresetQuestions = new ObservableCollection<string>(questions);
        }

        private void CreateSession()
        {
            ChatSession session = new ChatSession
            {
                id = nextSessionId++,
                issueCategory = "General",
                sessionStatus = "Open",
                startedAt = DateTime.Now,
                Title = $"Session {nextSessionId - 1}"
            };

            session.Messages.Add(new ChatMessage
            {
                Id = 1,
                SessionId = session.id,
                SenderType = "CHATBOT ASSISTANCE",
                Content = "Welcome. This support assistant uses preset questions and fixed answers only. Choose a question below or contact the real team at any time.",
                SentAt = DateTime.Now
            });

            UpdateSessionSummary(session);
            Sessions.Insert(0, session);
            CurrentSession = session;
            StatusMessage = "A new chat session is ready.";
        }

        private void UpdateSessionSummary(ChatSession session, string? selectedQuestion = null, string? response = null)
        {
            string preferredTitle = !string.IsNullOrWhiteSpace(selectedQuestion)
                ? selectedQuestion
                : session.Messages.FirstOrDefault(message => message.SenderType == "USER")?.Content ?? $"Session {session.id}";

            session.Title = TrimForPreview(preferredTitle, 32);

            string preferredPreview = !string.IsNullOrWhiteSpace(response)
                ? response
                : session.Messages.LastOrDefault()?.Content ?? "No messages yet.";

            session.LastPreview = TrimForPreview(preferredPreview, 56);
            session.LastUpdatedAt = DateTime.Now;
        }

        private static string TrimForPreview(string content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            string trimmed = content.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : $"{trimmed.Substring(0, maxLength - 3)}...";
        }

        private static string InferCategory(string question)
        {
            if (question.Contains("password", StringComparison.OrdinalIgnoreCase))
            {
                return "Account";
            }

            if (question.Contains("card", StringComparison.OrdinalIgnoreCase))
            {
                return "Cards";
            }

            if (question.Contains("transfer", StringComparison.OrdinalIgnoreCase))
            {
                return "Transfers";
            }

            if (question.Contains("technical", StringComparison.OrdinalIgnoreCase))
            {
                return "Technical Issue";
            }

            return "Other";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
