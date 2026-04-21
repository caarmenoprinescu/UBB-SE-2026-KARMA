// <copyright file="AccountStatusToBrushConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

public class AccountStatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value?.ToString() switch
        {
            "Active" => new SolidColorBrush(Color.FromArgb(255, 29, 185, 84)), // green
            "Closed" => new SolidColorBrush(Color.FromArgb(255, 229, 57, 53)), // red
            "Matured" => new SolidColorBrush(Color.FromArgb(255, 30, 136, 229)), // blue
            _ => new SolidColorBrush(Colors.Gray),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}