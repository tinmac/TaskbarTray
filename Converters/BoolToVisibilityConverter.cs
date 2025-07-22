using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace PowerSwitch.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isVisible = value is bool b && b;
            if (Invert)
                isVisible = !isVisible;
            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
            {
                bool result = v == Visibility.Visible;
                return Invert ? !result : result;
            }
            return false;
        }
    }

    public class BooleanToVisibilityInverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
                return Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
                return v != Visibility.Visible;
            return false;
        }
    }
}
