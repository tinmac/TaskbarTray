using System;
using System.Collections;
using Microsoft.UI.Xaml.Data;

namespace PowerSwitch.Converters
{
    public class SensorTypeAllowedConverter : IValueConverter
    {
        // value: AllowedSensorTypes (List<string>), parameter: sensor type (string)
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var allowed = value as IList;
            var sensorType = parameter as string;
            return allowed != null && sensorType != null && allowed.Contains(sensorType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // ConvertBack logic will be handled in the ViewModel via event
            return null;
        }
    }
}
