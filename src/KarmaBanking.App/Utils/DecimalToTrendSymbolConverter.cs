// <copyright file="DecimalToTrendSymbolConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

using System;
using Microsoft.UI.Xaml.Data;

namespace KarmaBanking.App.Utils;

public class DecimalToTrendSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal amount)
        {
            if (amount > 0)
            {
                return "▲";
            }

            if (amount < 0)
            {
                return "▼";
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}