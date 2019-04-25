using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ImageEditor.Views.Converters
{
    public class RasterGraphicNameToVisibilityConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s && s == "Pixel copying")
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
