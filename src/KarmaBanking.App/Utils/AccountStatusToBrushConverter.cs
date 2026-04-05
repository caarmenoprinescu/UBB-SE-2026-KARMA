using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace KarmaBanking.App.Utils
{
    public class AccountStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() switch
            {
                "Active" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 29, 185, 84)),   // green
                "Closed" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 229, 57,  53)),  // red
                "Locked" => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 140, 0)),   // orange
                _        => new SolidColorBrush(Colors.Gray)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
