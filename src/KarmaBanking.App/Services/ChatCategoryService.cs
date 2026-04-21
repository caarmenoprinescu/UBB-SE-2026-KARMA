// <copyright file="ChatCategoryService.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Services;

using System;

public class ChatCategoryService
{
    public string InferCategory(string question)
    {
        if (question.Contains("password", StringComparison.OrdinalIgnoreCase))
        {
            return "Account";
        }

        if (question.Contains("card", StringComparison.OrdinalIgnoreCase))
        {
            return "Cards";
        }

        if (question.Contains("transfer", StringComparison.OrdinalIgnoreCase))
        {
            return "Transfers";
        }

        if (question.Contains("technical", StringComparison.OrdinalIgnoreCase))
        {
            return "Technical Issue";
        }

        return "Other";
    }
}