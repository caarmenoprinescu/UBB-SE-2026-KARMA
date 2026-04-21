// <copyright file="ChatMessageRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories;

using System;
using System.Collections.Generic;
using System.Data;
using KarmaBanking.App.Data;
using KarmaBanking.App.Models;
using Microsoft.Data.SqlClient;

/// <summary>
/// Provides direct SQL access for chat message retrieval.
/// </summary>
public class ChatMessageRepository
{
    /// <summary>
    /// Gets all chat messages for a given session identifier.
    /// </summary>
    /// <param name="sessionIdentificationNumber">The chat session identifier.</param>
    /// <returns>The messages belonging to the requested session.</returns>
    public List<ChatMessage> GetMessagesBySessionIdentificationNumber(int sessionIdentificationNumber)
    {
        List<ChatMessage> chatMessages = [];

        using (var sqlConnection = DatabaseConfig.GetDatabaseConnection())
        {
            sqlConnection.Open();

            const string query =
                "SELECT id, sessionId, senderType, content, sentAt FROM ChatMessage WHERE sessionId = @sessionId ORDER BY sentAt ASC";
            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.Add("@sessionId", SqlDbType.Int).Value = sessionIdentificationNumber;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                chatMessages.Add(
                    new ChatMessage
                    {
                        IdentificationNumber = (int)reader["id"],
                        SessionIdentificationNumber = (int)reader["sessionId"],
                        SenderType = reader["senderType"].ToString() ?? string.Empty,
                        Content = reader["content"].ToString() ?? string.Empty,
                        SentAt = (DateTime)reader["sentAt"],
                    });
            }
        }

        return chatMessages;
    }
}