using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfApp1   // phải trùng với xmlns:local trong XAML
{
    public class BoolToAlignConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public string UserHex { get; set; } = "#27AE60";
        public string AiHex { get; set; } = "#ECECEC";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hex = (value is bool b && b) ? UserHex : AiHex;
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class BoolToTextBrushConverter : IValueConverter
    {
        public string UserHex { get; set; } = "#FFFFFF";
        public string AiHex { get; set; } = "#2C3E50";
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hex = (value is bool b && b) ? UserHex : AiHex;
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
