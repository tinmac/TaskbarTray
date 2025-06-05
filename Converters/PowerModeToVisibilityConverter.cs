using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskbarTray.Views;

namespace TaskbarTray.Converters
{

    public class PowerModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var selected = (PowerMode)value;
            var target = (PowerMode)Enum.Parse(typeof(PowerMode), (string)parameter);

            var ret = selected == target ? Visibility.Visible : Visibility.Collapsed;

            Debug.WriteLine($"value {value}  target {target}  return {ret}");
            
            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
