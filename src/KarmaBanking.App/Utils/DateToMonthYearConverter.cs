using Microsoft.UI.Xaml.Data;
using System;

namespace KarmaBanking.App.Utils
{
    public class DateToMonthYearConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime date)
            {
                // Format: "Mar '26"
                return date.ToString("MMM ''yy");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
