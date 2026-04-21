// <copyright file="PagedResult.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Models.DTOs;

using System.Collections.Generic;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}