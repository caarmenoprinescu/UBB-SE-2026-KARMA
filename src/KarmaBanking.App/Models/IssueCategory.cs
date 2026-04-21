// <copyright file="IssueCategory.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models;

/// <summary>
/// Defines high-level issue categories for support chat routing.
/// </summary>
public enum IssueCategory
{
    /// <summary>Account related issues.</summary>
    Account,

    /// <summary>Card related issues.</summary>
    Cards,

    /// <summary>Transfer related issues.</summary>
    Transfers,

    /// <summary>Loan related issues.</summary>
    Loans,

    /// <summary>Application or system technical issues.</summary>
    TechnicalIssue,

    /// <summary>Any issue not covered by predefined categories.</summary>
    Other,
}