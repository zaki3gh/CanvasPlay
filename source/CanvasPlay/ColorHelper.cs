using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MyApps.CanvasPlay
{
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value as Windows.UI.Color?;
            if ((color == null) || !color.HasValue)
            {
                return null;
            }

            return new Windows.UI.Xaml.Media.SolidColorBrush(color.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    /// <summary>
    ///  <c>Windows.UI.Color</c>を色設定用のデータバインディングで使えるようにするラッパー.
    /// </summary>
    public class ColorValue : Common.BindableBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public ColorValue() : this(Windows.UI.Colors.Transparent)
        {
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="color">色の初期値</param>
        public ColorValue(Windows.UI.Color color)
        {
            this.color = color;
        }

        /// <summary>
        ///  Colorプロパティ用フィールド.
        /// </summary>
        private Windows.UI.Color color;

        /// <summary>
        ///  <c>Windows.UI.Color</c>.
        /// </summary>
        public Windows.UI.Color Color
        {
            get { return this.color; }
            set
            {
                bool rChanged = this.color.R != value.R;
                bool gChanged = this.color.G != value.G;
                bool bChanged = this.color.B != value.B;
                bool aChanged = this.color.A != value.A;

                SetProperty(ref this.color, value);
                if (rChanged) { OnPropertyChanged("R"); }
                if (gChanged) { OnPropertyChanged("G"); }
                if (bChanged) { OnPropertyChanged("B"); }
                if (aChanged) { OnPropertyChanged("A"); }
            }
        }

        /// <summary>
        ///  <c>Windows.UI.Color</c>のR.
        /// </summary>
        public byte R
        {
            get { return this.Color.R; }
            set
            {
                if (this.color.R == value) 
                {
                    return;
                }

                this.color.R = value;
                OnPropertyChanged("Color");
                OnPropertyChanged("R");
            }
        }

        /// <summary>
        ///  <c>Windows.UI.Color</c>のG.
        /// </summary>
        public byte G
        {
            get { return this.Color.G; }
            set
            {
                if (this.color.G == value)
                {
                    return;
                }

                this.color.G = value;
                OnPropertyChanged("Color");
                OnPropertyChanged("G");
            }
        }

        /// <summary>
        ///  <c>Windows.UI.Color</c>のB.
        /// </summary>
        public byte B
        {
            get { return this.Color.B; }
            set
            {
                if (this.color.B == value)
                {
                    return;
                }

                this.color.B = value;
                OnPropertyChanged("Color");
                OnPropertyChanged("B");
            }
        }

        /// <summary>
        ///  <c>Windows.UI.Color</c>のA.
        /// </summary>
        public byte A
        {
            get { return this.Color.A; }
            set
            {
                if (this.color.A == value)
                {
                    return;
                }

                this.color.A = value;
                OnPropertyChanged("Color");
                OnPropertyChanged("A");
            }
        }
    }
}
