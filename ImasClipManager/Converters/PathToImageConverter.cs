using System;
using System.Globalization;
using System.Windows.Data;
using ImasClipManager.Helpers;

namespace ImasClipManager.Converters
{
    public class PathToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                return ImageHelper.LoadBitmapNoLock(path);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}