using Microsoft.UI.Xaml.Data;
using System;

namespace KarmaBanking.App.Utils
{
    public class DecimalToCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal d)
            {
                return d.ToString("C2");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
