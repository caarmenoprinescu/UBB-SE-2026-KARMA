using KarmaBanking.App.Data;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using KarmaBanking.App.Data;

namespace KarmaBanking.App.Repositories
{
    public class ChatSessionRepository
    {
        public void SaveSessionRatingAndFeedback(int sessionId, int rating, string feedback)
        {
            using (SqlConnection connection = new SqlConnection(DatabaseConfig.DatabaseConnectionString))
            {
                connection.Open();

                string query = @"UPDATE ChatSession
                             SET rating = @rating,
                                 feedback = @feedback,
                                 sessionStatus = @sessionStatus,
                                 endedAt = @endedAt
                             WHERE id = @sessionId";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.Add("@sessionId", SqlDbType.Int).Value = sessionId;
                command.Parameters.Add("@rating", SqlDbType.Int).Value = rating;
                command.Parameters.Add("@feedback", SqlDbType.NVarChar, 255).Value =
                    string.IsNullOrWhiteSpace(feedback) ? DBNull.Value : feedback;
                command.Parameters.Add("@sessionStatus", SqlDbType.NVarChar, 30).Value = "Closed";
                command.Parameters.Add("@endedAt", SqlDbType.DateTime2).Value = DateTime.Now;

                command.ExecuteNonQuery();
            }
        }

        public async Task<int> CreateChatSessionAsync(int userId, string issueCategory)
        {
            using (SqlConnection connection = DatabaseConfig.GetDatabaseConnection())
            {
                await connection.OpenAsync();

                string query = @"INSERT INTO ChatSession (userId, issueCategory, sessionStatus, startedAt)
                                 OUTPUT INSERTED.id
                                 VALUES (@userId, @issueCategory, @sessionStatus, @startedAt)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.Add("@userId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@issueCategory", SqlDbType.NVarChar, 50).Value = issueCategory;
                    command.Parameters.Add("@sessionStatus", SqlDbType.NVarChar, 30).Value = "Open";
                    command.Parameters.Add("@startedAt", SqlDbType.DateTime2).Value = DateTime.Now;

                    return (int)await command.ExecuteScalarAsync();
                }
            }
        }
    }
}