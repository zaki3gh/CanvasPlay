using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  色選択用の共通UI.
    /// </summary>
    public sealed partial class ColorPicker : UserControl
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public ColorPicker()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        ///  Static Constructor.
        /// </summary>
        static ColorPicker()
        {
            ColorProperty = DependencyProperty.Register("Color", typeof(ColorValue), typeof(ColorPicker),
                null);
            TitleProperty = DependencyProperty.Register("Title", typeof(String), typeof(ColorPicker),
                null);
            CloseButtonVisibilityProperty = DependencyProperty.Register("CloseButtonVisibility", typeof(Visibility), typeof(ColorPicker), 
                new PropertyMetadata(Visibility.Visible));
        }

        /// <summary>
        ///  <c>Color</c>プロパティ.
        /// </summary>
        public static DependencyProperty ColorProperty { get; private set; }

        /// <summary>
        ///  選択された色.
        /// </summary>
        public ColorValue Color
        {
            get { return (ColorValue)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        ///  Titleプロパティ.
        /// </summary>
        public static DependencyProperty TitleProperty { get; private set; }

        /// <summary>
        ///  タイトル.
        /// </summary>
        public String Title
        {
            get { return (String)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        ///  CloseButtonVisibilityプロパティ.
        /// </summary>
        public static DependencyProperty CloseButtonVisibilityProperty { get; private set; }

        /// <summary>
        ///  閉じるボタンの表示状態.
        /// </summary>
        public Visibility CloseButtonVisibility
        {
            get { return (Visibility)GetValue(CloseButtonVisibilityProperty); }
            set { SetValue(CloseButtonVisibilityProperty, value); }
        }

        /// <summary>
        ///  閉じるボタンがクリックされた時のイベント.
        /// </summary>
        public event EventHandler CloseButtonClicked;

        /// <summary>
        ///  <c>CloseButtonClicked</c>イベントを発生させる.
        /// </summary>
        private void OnCloseButtonClicked()
        {
            var handler = this.CloseButtonClicked;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        ///  一覧から選択のリストで選択された色が変わった時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void colorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = e.AddedItems.FirstOrDefault() as ColorValue;
            if (selected == null)
            {
                return;
            }

            this.Color.Color = selected.Color;
        }

        /// <summary>
        ///  閉じるボタンがクリックされた.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            OnCloseButtonClicked();
        }
    }
}
