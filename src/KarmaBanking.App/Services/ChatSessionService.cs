// <copyright file="ChatSessionService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Linq;
using KarmaBanking.App.Models;

public class ChatSessionService
{
    public ChatSession CreateSession(int sessionId)
    {
        var session = new ChatSession
        {
            IdentificationNumber = sessionId,
            IssueCategory = "General",
            SessionStatus = "Open",
            StartedAt = DateTime.Now,
            Title = $"Session {sessionId}",
        };

        session.Messages.Add(
            this.CreateMessage(session, 1, "CHATBOT ASSISTANCE", "Welcome. How can I help you?", DateTime.Now));
        return session;
    }

    public ChatMessage CreateMessage(
        ChatSession session,
        int messageId,
        string senderType,
        string content,
        DateTime sentAt)
    {
        return new ChatMessage
        {
            IdentificationNumber = messageId,
            SessionIdentificationNumber = session.IdentificationNumber,
            SenderType = senderType,
            Content = content,
            SentAt = sentAt,
        };
    }

    public string BuildTranscript(ChatSession? session)
    {
        if (session == null)
        {
            return "No chat session selected.";
        }

        var lines = session.Messages
            .Select(message => $"[{message.SentAt:g}] {message.SenderType}: {message.Content}")
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }
}