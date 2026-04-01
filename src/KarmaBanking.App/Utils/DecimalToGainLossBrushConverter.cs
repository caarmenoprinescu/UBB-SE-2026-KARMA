using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace KarmaBanking.App.Utils
{
    public class DecimalToGainLossBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush PositiveBrush = new SolidColorBrush(Colors.ForestGreen);
        private static readonly SolidColorBrush NegativeBrush = new SolidColorBrush(Colors.IndianRed);
        private static readonly SolidColorBrush NeutralBrush = new SolidColorBrush(Colors.Gray);

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
}
