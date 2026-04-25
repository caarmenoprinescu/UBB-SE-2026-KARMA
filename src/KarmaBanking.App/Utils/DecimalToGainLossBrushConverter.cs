// <copyright file="DecimalToGainLossBrushConverter.cs" company="Dev Core">
// Copyright (c) Dev Core. All rights reserved.
// </copyright>

namespace KarmaBanking.App.Utils;

using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Converts a decimal value to a <see cref="SolidColorBrush"/> indicating gain (green), loss (red), or neutral (gray).
/// </summary>
public class DecimalToGainLossBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush PositiveBrush = new(Colors.ForestGreen);
    private static readonly SolidColorBrush NegativeBrush = new(Colors.IndianRed);
    private static readonly SolidColorBrush NeutralBrush = new(Colors.Gray);

    /// <summary>
    /// Converts a decimal value to a color brush based on whether it is positive, negative, or zero.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>A <see cref="SolidColorBrush"/> representing the gain or loss.</returns>
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

    /// <summary>
    /// Converts a brush back to a decimal. This method is not implemented.
    /// </summary>
    /// <param name="value">The value that is produced by the binding target.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="parameter">The converter parameter to use.</param>
    /// <param name="language">The language of the conversion.</param>
    /// <returns>The converted value.</returns>
    /// <exception cref="NotImplementedException">Always thrown as two-way binding is not supported.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}