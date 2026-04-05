using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KarmaBanking.App.Models
{
    public class ChatSession : INotifyPropertyChanged
    {
        public int id { get; set; }
        public int userId { get; set; }
        public string issueCategory { get; set; } = string.Empty;
        public string sessionStatus { get; set; } = string.Empty;
        public int rating { get; set; }
        public string feedback { get; set; } = string.Empty;
        public DateTime startedAt { get; set; }
        public DateTime endedAt { get; set; }

        private string title = "New chat";
        private string lastPreview = "No messages yet.";
        private DateTime lastUpdatedAt = DateTime.Now;
        private bool isEscalatedToTeam;
        private string teamContactMessage = string.Empty;
        private SelectedAttachment? attachment;

        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastPreview
        {
            get => lastPreview;
            set
            {
                if (lastPreview != value)
                {
                    lastPreview = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime LastUpdatedAt
        {
            get => lastUpdatedAt;
            set
            {
                if (lastUpdatedAt != value)
                {
                    lastUpdatedAt = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LastUpdatedDisplay));
                }
            }
        }

        public string LastUpdatedDisplay => LastUpdatedAt.ToString("g");

        public bool IsEscalatedToTeam
        {
            get => isEscalatedToTeam;
            set
            {
                if (isEscalatedToTeam != value)
                {
                    isEscalatedToTeam = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SessionModeLabel));
                }
            }
        }

        public string SessionModeLabel => IsEscalatedToTeam ? "Team contact" : "Chatbot assistance";

        public string TeamContactMessage
        {
            get => teamContactMessage;
            set
            {
                if (teamContactMessage != value)
                {
                    teamContactMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public SelectedAttachment? Attachment
        {
            get => attachment;
            set
            {
                if (attachment != value)
                {
                    attachment = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
