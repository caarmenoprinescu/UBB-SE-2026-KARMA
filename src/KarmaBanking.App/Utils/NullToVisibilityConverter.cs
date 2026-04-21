// <copyright file="NullToVisibilityConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

/// <summary>
///     Returns Visible when the value is NOT null, Collapsed when null.
///     Pass parameter="Invert" to flip the logic.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isNull = value == null;
        var invert = parameter?.ToString() == "Invert";
        return isNull == invert ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}