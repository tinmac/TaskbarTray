using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;

namespace PowerSwitch.Converters;

public class BoolToImageSourceConverter : IValueConverter
{
    public ImageSource TrueImage { get; set; }
    public ImageSource FalseImage { get; set; }
    
    public object Convert(object value, Type targetType, object parameter, string language)
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
            var ret = value == TrueImage ? true : false;
            return ret;
        }

        return false;

        //throw new NotImplementedException();
    }
}
