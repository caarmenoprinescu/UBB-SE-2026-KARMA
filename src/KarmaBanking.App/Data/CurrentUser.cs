// <copyright file="CurrentUser.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

/// <summary>
/// Stores the currently authenticated user context for the app session.
/// </summary>
public static class CurrentUser
{
    /// <summary>
    /// Gets or sets the current user's identifier.
    /// </summary>
    public static int Id { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current user's display name.
    /// </summary>
    public static string Name { get; set; } = "Test User";
}