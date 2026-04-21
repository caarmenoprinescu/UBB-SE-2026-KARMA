using Microsoft.UI.Xaml.Data;
using System;

namespace KarmaBanking.App.Utils
{
    public class FilterMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string current = value?.ToString() ?? string.Empty;
            string expected = parameter?.ToString() ?? string.Empty;
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool isChecked = value is bool b && b;
            return isChecked ? parameter?.ToString() ?? string.Empty : string.Empty;
        }
    }
}

