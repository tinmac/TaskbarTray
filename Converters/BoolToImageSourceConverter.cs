using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace TaskbarTray.Converters;

public class BoolToImageSourceConverter : IValueConverter
{
    public ImageSource? TrueImage { get; set; }
    public ImageSource? FalseImage { get; set; }
    
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is true)
        {
            return TrueImage;
        }
        
        return FalseImage;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {

        if (value is BitmapImage)
        {
            return value == TrueImage ? true : false;
        }
        return false;

        //throw new NotImplementedException();
    }
}
