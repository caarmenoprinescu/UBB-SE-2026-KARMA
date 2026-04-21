// <copyright file="FilterMatchToBooleanConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Microsoft.UI.Xaml.Data;

public class FilterMatchToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var current = value?.ToString() ?? string.Empty;
        var expected = parameter?.ToString() ?? string.Empty;
        return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var isChecked = value is bool b && b;
        return isChecked ? parameter?.ToString() ?? string.Empty : string.Empty;
    }
}