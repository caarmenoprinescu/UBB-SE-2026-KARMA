// <copyright file="ChatMessage.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

using System;

public class ChatMessage
{
    public int IdentificationNumber { get; set; }

    public int SessionIdentificationNumber { get; set; }

    public string SenderType { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public string DisplaySentAt => this.SentAt.ToString("g");
}