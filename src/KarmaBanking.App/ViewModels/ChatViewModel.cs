namespace KarmaBanking.App.ViewModels
{
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

    public class ChatViewModel : INotifyPropertyChanged
    {
        public static ChatViewModel Instance { get; } = new ChatViewModel();

        private readonly ApiService apiService = new MockApiService();
        private ObservableCollection<ChatSession> chatSessions = [];
        private ObservableCollection<ChatMessage> chatMessages = [];
        private ObservableCollection<string> presetQuestions = [];
        private ChatSession? currentSession;
        private string statusMessage = "Choose a preset question to start a chat session.";
        private SelectedAttachment? selectedAttachment;
        private bool isUploading;
        private string uploadStatusMessage = "No file uploaded.";
        private int nextSessionIdentificationNumber = 1;

        private ChatViewModel()
        {
            StartNewSessionCommand = new RelayCommand(OnStartNewSessionAsync);
            RemoveAttachmentCommand = new RelayCommand(OnRemoveAttachmentAsync, () => CanRemoveAttachment);
            CreateSession();
            _ = LoadPresetQuestionsAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ChatSession> Sessions
        {
            get => chatSessions;
            set
            {
                if (chatSessions != value)
                {
                    chatSessions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<ChatMessage> Messages
        {
            get => chatMessages;
            set
            {
                if (chatMessages != value)
                {
                    chatMessages = value;
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
                    Messages = currentSession?.Messages ?? [];
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

        // --- THESE ARE THE MISSING METHODS ---
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

            EnsureSession();
            ChatSession session = CurrentSession!;
            session.IssueCategory = InferCategory(question);

            session.Messages.Add(new ChatMessage
            {
                IdentificationNumber = session.Messages.Count + 1,
                SessionIdentificationNumber = session.IdentificationNumber,
                SenderType = "USER",
                Content = question,
                SentAt = DateTime.Now,
            });

            session.Messages.Add(new ChatMessage
            {
                IdentificationNumber = session.Messages.Count + 1,
                SessionIdentificationNumber = session.IdentificationNumber,
                SenderType = "CHATBOT ASSISTANCE",
                Content = response,
                SentAt = DateTime.Now.AddSeconds(1),
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
            CurrentSession.SessionStatus = "Escalated";

            CurrentSession.Messages.Add(new ChatMessage
            {
                IdentificationNumber = CurrentSession.Messages.Count + 1,
                SessionIdentificationNumber = CurrentSession.IdentificationNumber,
                SenderType = "SYSTEM",
                Content = "Conversation sent to the Karma Banking team.",
                SentAt = DateTime.Now,
            });

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

            return string.Join(Environment.NewLine, lines);
        }

        public void EnsureSession()
        {
            if (CurrentSession == null)
            {
                CreateSession();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Task OnStartNewSessionAsync()
        {
            CreateSession();
            return Task.CompletedTask;
        }

        private Task OnRemoveAttachmentAsync()
        {
            if (!CanRemoveAttachment)
            {
                return Task.CompletedTask;
            }

            SelectedAttachment = null;
            UploadStatusMessage = "Attachment removed.";
            StatusMessage = "Attachment removed from the current chat session.";

            return Task.CompletedTask;
        }

        private async Task LoadPresetQuestionsAsync()
        {
            List<string> questions = await apiService.GetChatbotPresetQuestionsAsync();
            if (questions.Count > 0)
            {
                PresetQuestions = new ObservableCollection<string>(questions);
            }
        }

        private void CreateSession()
        {
            ChatSession session = new ChatSession
            {
                IdentificationNumber = nextSessionIdentificationNumber++,
                IssueCategory = "General",
                SessionStatus = "Open",
                StartedAt = DateTime.Now,
                Title = $"Session {nextSessionIdentificationNumber - 1}",
            };

            session.Messages.Add(new ChatMessage
            {
                IdentificationNumber = 1,
                SessionIdentificationNumber = session.IdentificationNumber,
                SenderType = "CHATBOT ASSISTANCE",
                Content = "Welcome. How can I help you?",
                SentAt = DateTime.Now,
            });

            Sessions.Insert(0, session);
            CurrentSession = session;
        }

        private void UpdateSessionSummary(ChatSession session, string? selectedQuestion = null, string? response = null)
        {
            session.LastUpdatedAt = DateTime.Now;
        }

        private string InferCategory(string question)
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
    }
}