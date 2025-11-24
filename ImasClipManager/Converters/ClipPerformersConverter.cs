using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using ImasClipManager.Models;

namespace ImasClipManager.Converters
{
    public class ClipPerformersConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Clip clip && clip.Performers != null)
            {
                if (!clip.Performers.Any()) return "";
                // 単純に Name プロパティを結合
                return string.Join(Environment.NewLine, clip.Performers.Select(p => p.Name));
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}