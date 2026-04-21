// <copyright file="ChatSessionRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories;

using System;
using System.Data;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using Microsoft.Data.SqlClient;

public class ChatSessionRepository
{
    public void SaveSessionRatingAndFeedback(int sessionId, int rating, string feedback)
    {
        using (var connection = new SqlConnection(DatabaseConfig.DatabaseConnectionString))
        {
            connection.Open();

            var query = @"UPDATE ChatSession
                             SET rating = @rating,
                                 feedback = @feedback,
                                 sessionStatus = @sessionStatus,
                                 endedAt = @endedAt
                             WHERE id = @sessionId";

            var command = new SqlCommand(query, connection);

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
        using (var connection = DatabaseConfig.GetDatabaseConnection())
        {
            await connection.OpenAsync();

            var query = @"INSERT INTO ChatSession (userId, issueCategory, sessionStatus, startedAt)
                                 OUTPUT INSERTED.id
                                 VALUES (@userId, @issueCategory, @sessionStatus, @startedAt)";

            using (var command = new SqlCommand(query, connection))
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