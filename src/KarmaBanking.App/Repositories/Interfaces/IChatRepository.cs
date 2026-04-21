// <copyright file="IChatRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories.Interfaces;

using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Models;

/// <summary>
/// Defines read operations for chat sessions and messages.
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Returns ordered messages for a chat session.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <returns>The list of messages in chronological order.</returns>
    Task<List<ChatMessage>> GetChatMessagesAsync(int chatSessionId);
}