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
    public List<ChatMessage> GetMessagesBySessionId(int sessionId)
    {
        List<ChatMessage> chatMessages = [];

        using (var sqlConnection = DatabaseConfig.GetDatabaseConnection())
        {
            sqlConnection.Open();

            const string query =
                "SELECT id, sessionId, senderType, content, sentAt FROM ChatMessage WHERE sessionId = @sessionId ORDER BY sentAt ASC";
            using var command = new SqlCommand(query, sqlConnection);
            command.Parameters.Add("@sessionId", SqlDbType.Int).Value = sessionId;

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                chatMessages.Add(
                    new ChatMessage
                    {
                        Id = (int)reader["id"],
                        SessionId = (int)reader["sessionId"],
                        SenderType = reader["senderType"].ToString() ?? string.Empty,
                        Content = reader["content"].ToString() ?? string.Empty,
                        SentAt = (DateTime)reader["sentAt"],
                    });
            }
        }

        return chatMessages;
    }
}