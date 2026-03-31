using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using KarmaBanking.App.Models;
using KarmaBanking.App.Utils;

namespace KarmaBanking.App.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private IssueCategory? selectedCategory;
        private string statusMessage = "Please select a category to continue.";
        private SelectedAttachment? selectedAttachment;
        private bool isUploading;
        private string uploadStatusMessage = "No file uploaded.";

        public IssueCategory? SelectedCategory
        {
            get => selectedCategory;
            set
            {
                if (selectedCategory != value)
                {
                    selectedCategory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanContinue));
                    ContinueCommand.RaiseCanExecuteChanged();
                }
            }
        }

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
                    OnPropertyChanged(nameof(CanRemoveAttachment));
                }
            }
        }

        public bool HasAttachmentPreview => SelectedAttachment != null;

        public bool CanContinue => SelectedCategory != null && !IsUploading;

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
                    OnPropertyChanged(nameof(CanContinue));
                    ContinueCommand.RaiseCanExecuteChanged();
                    RemoveAttachmentCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanAttachFile => !IsUploading;

        public bool CanRemoveAttachment => SelectedAttachment != null && !IsUploading;

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

        public RelayCommand ContinueCommand { get; }
        public RelayCommand RemoveAttachmentCommand { get; }

        public event Action<string>? ContinueRequested;

        public ChatViewModel()
        {
            ContinueCommand = new RelayCommand(OnContinueAsync, () => CanContinue);
            RemoveAttachmentCommand = new RelayCommand(OnRemoveAttachmentAsync, () => CanRemoveAttachment);
        }

        private Task OnContinueAsync()
        {
            if (SelectedCategory == null)
                return Task.CompletedTask;

            string categoryDisplayName = GetCategoryDisplayName(SelectedCategory.Value);
            StatusMessage = $"Selected category: {categoryDisplayName}";

            ContinueRequested?.Invoke(categoryDisplayName);

            return Task.CompletedTask;
        }

        private Task OnRemoveAttachmentAsync()
        {
            if (!CanRemoveAttachment)
                return Task.CompletedTask;

            SelectedAttachment = null;
            UploadStatusMessage = "Attachment removed.";
            StatusMessage = "Attachment removed.";

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

        private string GetCategoryDisplayName(IssueCategory category)
        {
            return category switch
            {
                IssueCategory.Account => "Account",
                IssueCategory.Cards => "Cards",
                IssueCategory.Transfers => "Transfers",
                IssueCategory.Loans => "Loans",
                IssueCategory.TechnicalIssue => "Technical Issue",
                IssueCategory.Other => "Other",
                _ => category.ToString()
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}