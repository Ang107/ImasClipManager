using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImasClipManager.Converters
{
    [ValueConversion(typeof(double), typeof(GridLength))]
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                return new GridLength(val);
            }
            return new GridLength(250);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GridLength gridLength)
            {
                return gridLength.Value;
            }
            return 250.0;
        }
    }
}