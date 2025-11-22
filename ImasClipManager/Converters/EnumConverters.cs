using System;
using System.Globalization;
using System.Windows.Data;
using ImasClipManager.Helpers; // 手順1の名前空間
using ImasClipManager.Models;

namespace ImasClipManager.Converters
{
    // LiveType用コンバーター
    public class LiveTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LiveType type)
            {
                return type.ToDisplayString();
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // BrandType用コンバーター
    public class BrandTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is BrandType brand)
            {
                return brand.ToDisplayString();
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}