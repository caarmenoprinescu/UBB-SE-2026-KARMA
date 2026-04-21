// <copyright file="MockApiService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

// Mock version of ApiService for testing without a real backend
// In ChatViewModel.cs replace your existing ApiService instance with the mock:
// _apiService = new MockApiService();
public class MockApiService : ApiService
{
    public override Task<List<string>> GetChatbotPresetQuestionsAsync()
    {
        return Task.FromResult(new List<string>(DefaultChatbotResponses.Keys));
    }

    public override Task<string> GetChatbotPresetAnswerAsync(string question)
    {
        if (DefaultChatbotResponses.TryGetValue(question, out var response))
        {
            return Task.FromResult(response);
        }

        return Task.FromResult("Please contact the team for more help with this topic.");
    }

    public override Task<bool> SendChatToSupportAsync(
        string transcript,
        string customerMessage,
        SelectedAttachment? attachment)
    {
        var hasPayload =
            !string.IsNullOrWhiteSpace(transcript) ||
            !string.IsNullOrWhiteSpace(customerMessage) ||
            attachment != null;

        return Task.FromResult(hasPayload);
    }

    public override Task<List<ChatMessage>?> GetChatHistoryAsync(int sessionId)
    {
        var messages = new List<ChatMessage>
        {
            new()
            {
                IdentificationNumber = 1,
                SessionIdentificationNumber = sessionId,
                SenderType = "USER",
                Content = "Hi, I need help with my account.",
                SentAt = DateTime.Now.AddMinutes(-15),
            },
            new()
            {
                IdentificationNumber = 2,
                SessionIdentificationNumber = sessionId,
                SenderType = "CONSULTANT",
                Content = "Hello! How can I assist you today?",
                SentAt = DateTime.Now.AddMinutes(-14),
            },
            new()
            {
                IdentificationNumber = 3,
                SessionIdentificationNumber = sessionId,
                SenderType = "USER",
                Content = "I want to check my balance.",
                SentAt = DateTime.Now.AddMinutes(-10),
            },
            new()
            {
                IdentificationNumber = 4,
                SessionIdentificationNumber = sessionId,
                SenderType = "CONSULTANT",
                Content = "Sure! Your current balance is $1,245.50.",
                SentAt = DateTime.Now.AddMinutes(-8),
            },
            new()
            {
                IdentificationNumber = 5,
                SessionIdentificationNumber = sessionId,
                SenderType = "USER",
                Content = "Thanks!",
                SentAt = DateTime.Now.AddMinutes(-5),
            },
            new()
            {
                IdentificationNumber = 6,
                SessionIdentificationNumber = sessionId,
                SenderType = "CONSULTANT",
                Content = "You're welcome! Anything else I can help with?",
                SentAt = DateTime.Now.AddMinutes(-2),
            },
        };

        return Task.FromResult<List<ChatMessage>?>(messages);
    }
}