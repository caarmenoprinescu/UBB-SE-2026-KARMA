// <copyright file="ChatSession.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ChatSession : INotifyPropertyChanged
{
    private SelectedAttachment? attachment;
    private bool isEscalatedToTeam;
    private string lastPreview = "No messages yet.";
    private DateTime lastUpdatedAt = DateTime.Now;
    private string teamContactMessage = string.Empty;
    private string title = "New chat";

    public int IdentificationNumber { get; set; }

    public string IssueCategory { get; set; } = string.Empty;

    public string SessionStatus { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public ObservableCollection<ChatMessage> Messages { get; set; } = [];

    public string Title
    {
        get => this.title;
        set
        {
            if (this.title != value)
            {
                this.title = value;
                this.OnPropertyChanged();
            }
        }
    }

    public string LastPreview
    {
        get => this.lastPreview;
        set
        {
            if (this.lastPreview != value)
            {
                this.lastPreview = value;
                this.OnPropertyChanged();
            }
        }
    }

    public DateTime LastUpdatedAt
    {
        get => this.lastUpdatedAt;
        set
        {
            if (this.lastUpdatedAt != value)
            {
                this.lastUpdatedAt = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.LastUpdatedDisplay));
            }
        }
    }

    public string LastUpdatedDisplay => this.LastUpdatedAt.ToString("g");

    public bool IsEscalatedToTeam
    {
        get => this.isEscalatedToTeam;
        set
        {
            if (this.isEscalatedToTeam != value)
            {
                this.isEscalatedToTeam = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.SessionModeLabel));
            }
        }
    }

    public string SessionModeLabel => this.IsEscalatedToTeam ? "Team contact" : "Chatbot assistance";

    public string TeamContactMessage
    {
        get => this.teamContactMessage;
        set
        {
            if (this.teamContactMessage != value)
            {
                this.teamContactMessage = value;
                this.OnPropertyChanged();
            }
        }
    }

    public SelectedAttachment? Attachment
    {
        get => this.attachment;
        set
        {
            if (this.attachment != value)
            {
                this.attachment = value;
                this.OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}