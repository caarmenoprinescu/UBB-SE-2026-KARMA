// <copyright file="ChatRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories;

using System.Collections.Generic;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

/// <summary>
/// SQL-backed chat repository implementation.
/// </summary>
public class ChatRepository : IChatRepository
{
    /// <summary>
    /// Gets all messages for a chat session ordered by sent timestamp.
    /// </summary>
    /// <param name="chatSessionId">The chat session identifier.</param>
    /// <returns>The messages that belong to the session.</returns>
    public async Task<List<ChatMessage>> GetChatMessagesAsync(int chatSessionId)
    {
        var messages = new List<ChatMessage>();

        using (var conn = DatabaseConfig.GetDatabaseConnection())
        {
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "SELECT id, sessionId, senderType, content, sentAt " +
                "FROM ChatMessage " +
                "WHERE sessionId = @chatId " +
                "ORDER BY sentAt",
                conn);

            cmd.Parameters.AddWithValue("@chatId", chatSessionId);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    messages.Add(
                        new ChatMessage
                        {
                            IdentificationNumber = reader.GetInt32(0),
                            SessionIdentificationNumber = reader.GetInt32(1),
                            SenderType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Content = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            SentAt = reader.GetDateTime(4),
                        });
                }
            }
        }

        return messages;
    }
}