using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MultiLogViewer.ViewModels.Converters
{
    public class MultilineToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string message && !string.IsNullOrEmpty(message))
            {
                if (parameter as string == "NotEmpty")
                {
                    return Visibility.Visible;
                }
                return message.IndexOfAny(new[] { '\r', '\n' }) >= 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
