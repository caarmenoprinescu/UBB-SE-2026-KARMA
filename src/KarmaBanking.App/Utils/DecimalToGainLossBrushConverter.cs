// <copyright file="DecimalToGainLossBrushConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

public class DecimalToGainLossBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush PositiveBrush = new(Colors.ForestGreen);
    private static readonly SolidColorBrush NegativeBrush = new(Colors.IndianRed);
    private static readonly SolidColorBrush NeutralBrush = new(Colors.Gray);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is decimal amount)
        {
            if (amount > 0)
            {
                return PositiveBrush;
            }

            if (amount < 0)
            {
                return NegativeBrush;
            }
        }

        return NeutralBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}