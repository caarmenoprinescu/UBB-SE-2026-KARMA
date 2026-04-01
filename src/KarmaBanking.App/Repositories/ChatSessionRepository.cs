using System;
using System.Data.SqlClient;
using System.Data;

public class ChatSessionRepository 
{
    public void SaveSessionRatingAndFeedback(int sessionId, int rating, string feedback)
    {
        using (SqlConnection connection = new SqlConnection(DatabaseConfig.ConnectionString))
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
}