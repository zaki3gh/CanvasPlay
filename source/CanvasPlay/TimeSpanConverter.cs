using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MyApps.CanvasPlay
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var ts = value as TimeSpan?;
            if ((ts == null) || !ts.HasValue)
            {
                return String.Empty;
            }

            var fmt = parameter as string;
            if (fmt != null)
            {
                return ts.Value.ToString(fmt);
            }
            else
            {
                return ts.Value.ToString("mm\\:ss\\.f");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return TimeSpan.Parse(value as string);
        }
    }
}
