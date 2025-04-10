using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MhtToPdfConverter.Converters
{
    /// <summary>
    /// Converts a Boolean value to its inverse Visibility value.
    /// True returns Collapsed, False returns Visible.
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = false;
            if (value is bool b)
            {
                boolValue = b;
            }

            // Inverse logic: True -> Collapsed, False -> Visible
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack is typically not needed for one-way bindings like visibility
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed; // Collapsed means original was true
            }
            return false;
        }
    }
}
