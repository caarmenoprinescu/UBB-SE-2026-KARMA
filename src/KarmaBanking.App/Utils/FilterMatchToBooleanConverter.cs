// <copyright file="FilterMatchToBooleanConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Microsoft.UI.Xaml.Data;

/// <summary>
/// Compares a bound value against a parameter and returns true if they match.
/// </summary>
public class FilterMatchToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Checks if the bound value matches the converter parameter.
    /// </summary>
    /// <param name="value">The current filter value.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The expected filter value to match against.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>True if the values match; otherwise, false.</returns>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var current = value?.ToString() ?? string.Empty;
        var expected = parameter?.ToString() ?? string.Empty;
        return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the converter parameter if the bound value is true.
    /// </summary>
    /// <param name="value">The boolean value indicating if this option is selected.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>The parameter string if checked; otherwise, an empty string.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var isChecked = value is bool b && b;
        return isChecked ? parameter?.ToString() ?? string.Empty : string.Empty;
    }
}