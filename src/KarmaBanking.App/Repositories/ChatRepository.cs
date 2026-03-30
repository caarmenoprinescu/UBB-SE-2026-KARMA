using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KarmaBanking.App.Repositories
{
    public class ChatRepository : IChatRepository
    {
        public async Task<List<ChatMessage>> GetChatMessagesAsync(int chatSessionId)
        {
            var messages = new List<ChatMessage>();

            using (var conn = DatabaseConfig.GetDatabaseConnection())
            {
                await conn.OpenAsync();

                var cmd = new SqlCommand(
                    "SELECT Id, SenderType, SenderId, SentAt, AttachmentType " +
                    "FROM ChatMessages " +
                    "WHERE ChatSessionId = @chatId " +
                    "ORDER BY SentAt",
                    conn
                );
                cmd.Parameters.AddWithValue("@chatId", chatSessionId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        messages.Add(new ChatMessage
                        {
                            Id = reader.GetInt32(0),
                            SenderType = reader.GetString(1),
                            SenderId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                            SentAt = reader.GetDateTime(3),
                            AttachmentType = reader.IsDBNull(4) ? null : reader.GetString(4)
                        });
                    }
                }
            }

            return messages;
        }
    }
}