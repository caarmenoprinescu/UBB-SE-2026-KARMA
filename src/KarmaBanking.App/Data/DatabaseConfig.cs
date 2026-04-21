// <copyright file="DatabaseConfig.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Data;

using Microsoft.Data.SqlClient;

/// <summary>
/// Provides centralized database connection settings and factory helpers.
/// </summary>
public static class DatabaseConfig
{
    /// <summary>
    /// The SQL Server connection string used by the application.
    /// </summary>
    public static readonly string DatabaseConnectionString =
        @"Server=localhost\SQLEXPRESS;Database=KarmaBankingDb;Trusted_Connection=True;TrustServerCertificate=True;";

    /// <summary>
    /// Creates a new SQL connection using <see cref="DatabaseConnectionString"/>.
    /// </summary>
    /// <returns>A new <see cref="SqlConnection"/> instance.</returns>
    public static SqlConnection GetDatabaseConnection()
    {
        return new SqlConnection(DatabaseConnectionString);
    }
}