// <copyright file="ChatSessionRepository.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Repositories;

using System;
using System.Data;
using System.Threading.Tasks;
using KarmaBanking.App.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// Handles persistence operations for chat session lifecycle data.
/// </summary>
public class ChatSessionRepository
{
    /// <summary>
    /// Persists end-of-session rating and feedback for a chat session.
    /// </summary>
    /// <param name="sessionId">The chat session identifier.</param>
    /// <param name="rating">The user-provided rating.</param>
    /// <param name="feedback">The optional user feedback text.</param>
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

    /// <summary>
    /// Creates a new chat session and returns its generated identifier.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="issueCategory">The issue category used for routing.</param>
    /// <returns>The created chat session identifier.</returns>
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