// <copyright file="ChatSession.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a customer support chat conversation and its UI metadata.
/// </summary>
public class ChatSession : INotifyPropertyChanged
{
    private SelectedAttachment? attachment;
    private bool isEscalatedToTeam;
    private string lastPreview = "No messages yet.";
    private DateTime lastUpdatedAt = DateTime.Now;
    private string teamContactMessage = string.Empty;
    private string title = "New chat";

    /// <summary>
    /// Gets or sets the unique session identifier.
    /// </summary>
    public int IdentificationNumber { get; set; }

    /// <summary>
    /// Gets or sets the mapped issue category for routing.
    /// </summary>
    public string IssueCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status label of the chat session.
    /// </summary>
    public string SessionStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the session started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the message collection for this session.
    /// </summary>
    public ObservableCollection<ChatMessage> Messages { get; set; } = [];

    /// <summary>
    /// Gets or sets the UI title displayed for the session.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a short preview text of the latest message.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the timestamp of the latest session update.
    /// </summary>
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

    /// <summary>
    /// Gets a localized display value for <see cref="LastUpdatedAt"/>.
    /// </summary>
    public string LastUpdatedDisplay => this.LastUpdatedAt.ToString("g");

    /// <summary>
    /// Gets or sets a value indicating whether the session was escalated to a human team.
    /// </summary>
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

    /// <summary>
    /// Gets the current assistance mode label shown in the UI.
    /// </summary>
    public string SessionModeLabel => this.IsEscalatedToTeam ? "Team contact" : "Chatbot assistance";

    /// <summary>
    /// Gets or sets an optional explanatory message for team contact.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the currently selected attachment for the conversation.
    /// </summary>
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

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the property changed notification for bound UI listeners.
    /// </summary>
    /// <param name="propertyName">The changed property name.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}