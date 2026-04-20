namespace KarmaBanking.App.Repositories
{
    using KarmaBanking.App.Data;
    using KarmaBanking.App.Models;
    using Microsoft.Data.SqlClient;
    using System;
    using System.Collections.Generic;
    using System.Data;

    public class ChatMessageRepository
    {
        public List<ChatMessage> GetMessagesBySessionIdentificationNumber(int sessionIdentificationNumber)
        {
            List<ChatMessage> chatMessages = [];

            using (SqlConnection sqlConnection = DatabaseConfig.GetDatabaseConnection())
            {
                sqlConnection.Open();

                const string query = "SELECT id, sessionId, senderType, content, sentAt FROM ChatMessage WHERE sessionId = @sessionId ORDER BY sentAt ASC";
                using SqlCommand command = new SqlCommand(query, sqlConnection);
                command.Parameters.Add("@sessionId", SqlDbType.Int).Value = sessionIdentificationNumber;

                using SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    chatMessages.Add(new ChatMessage
                    {
                        IdentificationNumber = (int)reader["id"],
                        SessionIdentificationNumber = (int)reader["sessionId"],
                        SenderType = reader["senderType"].ToString() ?? string.Empty,
                        Content = reader["content"].ToString() ?? string.Empty,
                        SentAt = (DateTime)reader["sentAt"]
                    });
                }
            }

            return chatMessages;
        }
    }
}