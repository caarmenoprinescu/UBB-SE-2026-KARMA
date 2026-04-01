using Microsoft.UI.Xaml.Data;
using System;

namespace KarmaBanking.App.Utils
{
    public class DecimalToPercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal amount)
            {
                return $"{amount:F2}%";
            }

            return "0.00%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
