using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace KarmaBanking.App.Utils
{
    /// <summary>
    /// Returns Visible when the value is NOT null, Collapsed when null.
    /// Pass parameter="Invert" to flip the logic.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isNull = value == null;
            bool invert = parameter?.ToString() == "Invert";
            return (isNull == invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
