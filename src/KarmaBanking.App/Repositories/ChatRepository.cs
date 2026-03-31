using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;
using Microsoft.Data.SqlClient;
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
                    "SELECT id, sessionId, senderType, content, sentAt " +
                    "FROM ChatMessage " +
                    "WHERE sessionId = @chatId " +
                    "ORDER BY sentAt",
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
                            SessionId = reader.GetInt32(1),
                            SenderType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Content = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            SentAt = reader.GetDateTime(4)
                        });
                    }
                }
            }

            return messages;
        }
    }
}