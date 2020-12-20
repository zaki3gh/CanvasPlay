using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MyApps.CanvasPlay
{
    public class ByteDoubleConverter : IValueConverter
    {
        public ByteDoubleConverter() : this(1.0, 0.0)
        {
        }

        public ByteDoubleConverter(double scale, double translationAfterScaling)
        {
            this.Scale = 1.0;
            this.TranslationAfterScaling = 0.0;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var by = value as byte?;
            var d = System.Convert.ToDouble(by.GetValueOrDefault());
            if (this.ShouldCalc)
            {
                d = d * this.Scale + this.TranslationAfterScaling;
            }
            return d;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var d = value as double?;
            if (this.ShouldCalc)
            {
                d = (d - this.TranslationAfterScaling) / this.Scale;
            }
            return System.Convert.ToByte(d.GetValueOrDefault());
        }

        private bool ShouldCalc
        {
            get
            {
                return (this.Scale != 1.0) || (this.TranslationAfterScaling != 0.0);
            }
        }

        public double Scale { get; set; }
        public double TranslationAfterScaling { get; set; }
    }


}
