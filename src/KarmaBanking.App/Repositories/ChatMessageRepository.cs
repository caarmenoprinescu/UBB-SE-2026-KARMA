using KarmaBanking.App.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public class ChatMessageRepository
{
    public List<ChatMessage> GetMessagesBySessionId(int sessionId)
    {
        List<ChatMessage> messages = new List<ChatMessage>();

        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
        {
            connection.Open();

            string query = @"SELECT id, sessionId, senderType, content, sentAt
                             FROM ChatMessage
                             WHERE sessionId = @sessionId
                             ORDER BY sentAt ASC";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.Add("@sessionId", SqlDbType.Int).Value = sessionId;

            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                ChatMessage message = new ChatMessage
                {
                    Id = (int)reader["id"],
                    SessionId = (int)reader["sessionId"],
                    SenderType = reader["senderType"].ToString(),
                    Content = reader["content"].ToString(),
                    SentAt = (DateTime)reader["sentAt"]
                };

                messages.Add(message);
            }
        }

        return messages;
    }
}