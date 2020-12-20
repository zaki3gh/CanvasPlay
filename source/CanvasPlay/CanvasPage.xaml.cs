using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Json;
using Windows.Storage.Pickers;

// 基本ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234237 を参照してください

namespace MyApps.CanvasPlay
{
    /// <summary>
    /// 多くのアプリケーションに共通の特性を指定する基本ページ。
    /// </summary>
    public sealed partial class CanvasPage : MyApps.CanvasPlay.Common.LayoutAwarePage
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public CanvasPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// このページには、移動中に渡されるコンテンツを設定します。前のセッションからページを
        /// 再作成する場合は、保存状態も指定されます。
        /// </summary>
        /// <param name="navigationParameter">このページが最初に要求されたときに
        /// <see cref="Frame.Navigate(Type, Object)"/> に渡されたパラメーター値。
        /// </param>
        /// <param name="pageState">前のセッションでこのページによって保存された状態の
        /// ディクショナリ。ページに初めてアクセスするとき、状態は null になります。</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState != null)
            {
                if (CanvasItemManager.Current.Items.Count == 0)
                {
                    await CanvasItemManager.Current.LoadAllItemsAsync();
                }

                if ((pageState[PageStateKeyHasNewTraces] as bool?).GetValueOrDefault())
                {
                    var msg = new Windows.UI.Popups.MessageDialog(App.CurrentApp.MyResources["MessageCancelItemBecauseTerminated"]);
                    await msg.ShowAsync();
                }

                var item = CanvasItemManager.Current.GetItemByInternalName(pageState[PageStateKeyItemInternalName] as string);
                if (item == null)
                {
                    if (CanvasItemManager.Current.Items.Count != 0)
                    {
                        this.Frame.GoBack();
                        return;
                    }
                    else
                    {
                        item = new CanvasItem();
                    }
                }
                this.MainCanvas.Item = item;

                if (GetPageStateValue(pageState, PageStateKeyEditMode, false))
                {
                    this.MainCanvas.IsEditing = true;
                }
                
                if (GetPageStateValue(pageState, PageStateKeyEditToolPanelIsPinned, false))
                {
                    this.editToolPanel_PinButton.IsChecked = true;
                    this.TopAppBar.IsOpen = true;
                }
            }
            else
            {
                var nav = new CanvasPageNavigation();
                if (!nav.Deserialize(navigationParameter as string))
                {
                    this.Frame.GoBack();
                    return;
                }

                this.MainCanvas.Item = nav.Item != null ? nav.Item : new CanvasItem();
                if (nav.Reason == CanvasPageNavigationReason.Play)
                {
                    this.MainCanvas.IsEditing = false;
                    Play();
                }
                else
                {
                    this.MainCanvas.IsEditing = true;
                }
            }
        }

        private T GetPageStateValue<T>(Dictionary<string, object> pageState, string key, T defaultValue)
        {
            object o;
            if (!pageState.TryGetValue(key, out o))
            {
                return defaultValue;
            }

            return (T)o;
        }

        /// <summary>
        /// アプリケーションが中断される場合、またはページがナビゲーション キャッシュから破棄される場合、
        /// このページに関連付けられた状態を保存します。値は、
        /// <see cref="SuspensionManager.SessionState"/> のシリアル化の要件に準拠する必要があります。
        /// </summary>
        /// <param name="pageState">シリアル化可能な状態で作成される空のディクショナリ。</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            if (this.Item != null)
            {
                pageState[PageStateKeyItemInternalName] = this.Item.InternalName;
                pageState[PageStateKeyHasNewTraces] = this.MainCanvas.HasNewTraces;
                pageState[PageStateKeyEditMode] = this.MainCanvas.IsEditing;
                pageState[PageStateKeyEditToolPanelIsPinned] = this.editToolPanel_PinButton.IsChecked.GetValueOrDefault();
            }
        }

        /// <summary>
        ///  アプリケーションの実行が再開されるときの処理を行う.
        /// </summary>
        public void OnResuming()
        {
            ResumePlay();
        }

        /// <summary>
        ///  このページが表示されなくなるときの処理を行う.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            //Stop();
            Pause();
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        ///  ページに関連付けられている情報 - 手描き入力アイテムの内部名.
        /// </summary>
        private const string PageStateKeyItemInternalName = "ItemInternalName";

        /// <summary>
        ///  ページに関連付けられている情報 - 手描き入力アイテムが編集されているかどうか.
        /// </summary>
        private const string PageStateKeyHasNewTraces = "HasNewTraces";

        private const string PageStateKeyEditMode = "EditMode";
        private const string PageStateKeyEditToolPanelIsPinned = "EditToolPanelIsPinned";

        /// <summary>
        ///  手描き入力アイテム.
        /// </summary>
        public CanvasItem Item
        {
            get { return this.MainCanvas.Item; }
        }

        /// <summary>
        ///  再生が一度でも開始されたかどうかを示す値.
        /// </summary>
        private bool isPlayingOrEndPlaying = false;

        /// <summary>
        ///  手描き入力を再生する.
        /// </summary>
        private void Play()
        {
            // 再生を始める前に編集(線の追加)をした場合には、
            // それらの編集を既存の入力の前に挿入する。
            // このためのフラグ操作である。
            if (this.isPlayingOrEndPlaying)
            {
                this.isPlayingOrEndPlaying = false;
                this.MainCanvas.MergeNewTraces();
                this.MainCanvas.ClearCanvas();
            }
            else
            {
                // ↑のチェックだけでは手描き->再生としたときに
                // 何も動作しないように見えてしまうので、既存の手描き入力がないなら
                // それを取り込みキャンバスをクリアして、ちゃんと再生できるように見せる.
                if (!this.Item.InputRecorder.HasTrace)
                {
                    this.MainCanvas.MergeNewTraces();
                    this.MainCanvas.ClearCanvas();
                }
            }
            this.MainCanvas.PlayAsync(0);
            this.isPlayingOrEndPlaying = true;
        }

        /// <summary>
        ///  手描き入力の再生を停止する.
        /// </summary>
        private void Stop()
        {
            this.MainCanvas.StopPlaying();
        }

        /// <summary>
        ///  一時停止した手描き入力の再生を再開する.
        /// </summary>
        private void ResumePlay()
        {
            this.MainCanvas.ResumePlay();
        }

        /// <summary>
        ///  手描き入力の再生を一時停止する.
        /// </summary>
        private void Pause()
        {
            this.MainCanvas.PausePlaying();
        }

        /// <summary>
        ///  変更を保存する.
        /// </summary>
        private async void SaveAsync()
        {
            this.MainCanvas.MergeNewTraces();
            await this.Item.SaveAsync();
            await this.CameraCache.ClearAsync();
            if (!CanvasItemManager.Current.Items.Contains(this.Item))
            {
                await CanvasItemManager.Current.AddItemAsync(this.Item);
            }
            this.isPlayingOrEndPlaying = true;
        }

        /// <summary>
        ///  変更を取り消す.
        /// </summary>
        private async void CancelAsync()
        {
            this.MainCanvas.CancelNewTraces();
            await this.Item.LoadAsync();
            await this.CameraCache.ClearAsync();
            this.MainCanvas.ClearCanvas();
        }

        #region Bottom AppBar

        /// <summary>
        ///  "編集モード"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            // ToggleButtonをAppBarで使うときにはworkaroundが必要らしい
            // http://www.visuallylocated.com/post/2012/09/04/Fixing-the-VisualState-of-your-AppBar-ToggleButton.aspx

            var btn = sender as ToggleButton;

            // if IsPlaying
            //   do nothing
            if (this.MainCanvas.IsPlaying)
            {
                return;
            }

            // EditMode off->on
            // - clear canvas
            if (!this.MainCanvas.IsEditing)
            {
                this.MainCanvas.ClearCanvas();
            }
            // EditMode on->off
            //   - merge new traces
            //   - clear canvas
            else
            {
                this.MainCanvas.MergeNewTraces();
                this.MainCanvas.ClearCanvas();
            }

            // 状態と値の更新は最後にまとめて行う
            VisualStateManager.GoToState(btn, btn.IsChecked.Value ? "Checked" : "Unchecked", false);
            this.MainCanvas.IsEditing = btn.IsChecked.Value;
        }

        /// <summary>
        ///  "保存"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAsync();
        }

        /// <summary>
        ///  "取消"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelAsync();
        }

        /// <summary>
        ///  "再生"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        /// <summary>
        ///  "停止"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        ///  BottomAppBarの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BottomAppBar_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var ui in this.bottomAppBarEditPanel.Children)
            {
                base.StartLayoutUpdates(ui, new RoutedEventArgs());
            }
            foreach (var ui in this.bottomAppBarPlayPanel.Children)
            {
                base.StartLayoutUpdates(ui, new RoutedEventArgs());
            }

            (sender as AppBar).DataContext = this.MainCanvas;
        }

        /// <summary>
        ///  AppBarの<c>Unloaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BottomAppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (var ui in this.bottomAppBarEditPanel.Children)
            {
                base.StopLayoutUpdates(ui, new RoutedEventArgs());
            }
            foreach (var ui in this.bottomAppBarPlayPanel.Children)
            {
                base.StopLayoutUpdates(ui, new RoutedEventArgs());
            }
        }

        #endregion

        #region Top AppBar

        /// <summary>
        ///  TopAppBarの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopAppBar_Loaded(object sender, RoutedEventArgs e)
        {
            this.topAppBarEditToolPanel.DataContext = this.MainCanvas;
            (sender as AppBar).DataContext = this.Item;

            foreach (var ui in this.topAppBarEditToolPanel.Children)
            {
                base.StartLayoutUpdates(ui, new RoutedEventArgs());
            }
        }

        /// <summary>
        ///  TopAppBarの<c>Unloaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopAppBar_Unloaded(object sender, RoutedEventArgs e)
        {
            foreach (var ui in this.topAppBarEditToolPanel.Children)
            {
                base.StopLayoutUpdates(ui, new RoutedEventArgs());
            }

            CloseOtherPopups(null);
        }

        /// <summary>
        ///  編集用ツールのAppBarのIsStickyを制御するトグルボタンの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditToolsPanel_PinButton_Click(object sender, RoutedEventArgs e)
        {
            // ToggleButtonをAppBarで使うときにはworkaroundが必要らしい
            // http://www.visuallylocated.com/post/2012/09/04/Fixing-the-VisualState-of-your-AppBar-ToggleButton.aspx

            var btn = sender as ToggleButton;
            VisualStateManager.GoToState(btn, btn.IsChecked.Value ? "Checked" : "Unchecked", false);

            if (!btn.IsChecked.GetValueOrDefault())
            {
                if (!this.BottomAppBar.IsOpen)
                {
                    this.TopAppBar.IsOpen = false;
                }
            }
        }

        /// <summary>
        ///  「線の色」ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditToolsPanel_StrokeColorButton_Click(object sender, RoutedEventArgs e)
        {
            this.strokeColorPicker.Color = new ColorValue(this.MainCanvas.StrokeColor);

            var button = sender as Button;
            var t = button.TransformToVisual(this);
            var p = t.TransformPoint(new Point(button.ActualWidth / 2.0, button.ActualHeight / 2.0));

            ShowColorPickerPopup(this.strokeColorPickerPopup, p);
        }

        /// <summary>
        ///  「背景色」ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditToolsPanel_BackgroundColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.MainCanvas.IsBackgroundSet)
            {
                this.backgroundColorPicker.Color = new ColorValue(this.MainCanvas.BackgroundColor);
            }
            else
            {
                this.backgroundColorPicker.Color = new ColorValue();
            }

            var button = sender as Button;
            var t = button.TransformToVisual(this);
            var p = t.TransformPoint(new Point(button.ActualWidth / 2.0, button.ActualHeight / 2.0));

            ShowColorPickerPopup(this.backgroundColorPickerPopup, p);
        }

        /// <summary>
        ///  色選択のポップアップを表示する.
        /// </summary>
        /// <param name="point"></param>
        private void ShowColorPickerPopup(Popup popup, Point point)
        {
            CloseOtherPopups(popup);

            if (popup.IsOpen)
            {
                popup.IsOpen = false;
                return;
            }

            popup.HorizontalOffset = point.X;
            popup.VerticalOffset = this.TopAppBar.ActualHeight;
            popup.IsOpen = true;
        }

        /// <summary>
        ///  指定したポップアップ以外を閉じる.
        /// </summary>
        /// <param name="popup"></param>
        private void CloseOtherPopups(Popup popup)
        {
            if (this.strokeColorPickerPopup != popup)
            {
                this.strokeColorPickerPopup.IsOpen = false;
            }
            if (this.backgroundColorPickerPopup != popup)
            {
                this.backgroundColorPickerPopup.IsOpen = false;
            }
        }

        /// <summary>
        ///  色選択のポップアップが表示されるときの位置調整を行う.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void colorPickerPopup_Border_Loaded(object sender, RoutedEventArgs e)
        {
            // adjust location
            var border = sender as Border;
            var popup = border.Parent as Popup;
            popup.HorizontalOffset -= border.ActualWidth / 2.0;
            if (popup.HorizontalOffset < 20)
            {
                popup.HorizontalOffset = 20;
            }
            else if (popup.HorizontalOffset + border.ActualWidth > this.ActualWidth - 20.0)
            {
                popup.HorizontalOffset = this.ActualWidth - border.ActualWidth - 20.0;
            }
        }

        /// <summary>
        ///  色選択のポップアップの閉じるボタンの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void colorPicker_CloseButtonClicked(object sender, EventArgs e)
        {
            var popup = (((sender as ColorPicker).Parent) as Border).Parent as Popup;
            popup.IsOpen = false;
        }

        /// <summary>
        ///  「カメラ」ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditToolsPanel_CameraButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureCameraAsync();
        }

        /// <summary>
        ///  カメラによる写真撮影を行う.
        /// </summary>
        private async void CaptureCameraAsync()
        {
            var cameraUI = new Windows.Media.Capture.CameraCaptureUI();
            var file = await cameraUI.CaptureFileAsync(Windows.Media.Capture.CameraCaptureUIMode.Photo);
            if (file == null)
            {
                return;
            }
            await this.CameraCache.MoveAndAddAsync(file);
            await this.MainCanvas.SetImageAsync(file);
        }

        /// <summary>
        ///  カメラで撮影したファイルの管理.
        /// </summary>
        private CameraCache CameraCache
        {
            get
            {
                if (this.cameraCache == null)
                {
                    this.cameraCache = new CameraCache("CameraCache");
                }

                return this.cameraCache;
            }
        }

        /// <summary>
        ///  <c>CameraCache</c>プロパティ.
        /// </summary>
        private CameraCache cameraCache;

        /// <summary>
        ///  "ファイルを開く"ボタンの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void EditToolsPanel_OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenFileAsync();
        }

        /// <summary>
        ///  ファイルを開いて背景に貼り付ける.
        /// </summary>
        /// <returns></returns>
        private async Task OpenFileAsync()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            picker.ViewMode = PickerViewMode.Thumbnail;

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            // 画像ファイル以外を排除
            var properties = await file.Properties.GetImagePropertiesAsync();
            if ((properties.Width == 0) || (properties.Height == 0))
            {
                return;
            }

            file = await this.CameraCache.CopyAndAddAsync(file);
            if (file == null)
            {
                return;
            }

            await this.MainCanvas.SetImageAsync(file);
        }

        #endregion

    }

    /// <summary>
    ///  CanvasPageを表示する理由.
    /// </summary>
    enum CanvasPageNavigationReason
    {
        /// <summary>
        ///  新規作成.
        /// </summary>
        New, 

        /// <summary>
        ///  再生.
        /// </summary>
        Play, 

        /// <summary>
        ///  編集.
        /// </summary>
        Edit, 
    }

    /// <summary>
    ///  CanvasPageへの遷移のパラメーター.
    /// </summary>
    class CanvasPageNavigation
    {
        /// <summary>
        ///  理由.
        /// </summary>
        public CanvasPageNavigationReason Reason{ get; set; }

        /// <summary>
        ///  CanvasPageに表示する手描き入力.
        /// </summary>
        public CanvasItem Item { get; set; }

        /// <summary>
        ///  文字列にする.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            var json = new JsonArray();
            json.Add(JsonValue.CreateStringValue(this.Reason.ToString()));
            json.Add(JsonValue.CreateStringValue(this.Item != null ? this.Item.InternalName : String.Empty));
            return json.Stringify();
        }

        /// <summary>
        ///  文字列から復元する.
        /// </summary>
        /// <param name="input">シリアライズされた文字列</param>
        /// <returns></returns>
        public bool Deserialize(string input)
        {
            JsonArray json;
            if (!JsonArray.TryParse(input, out json))
            {
                return false;
            }

            CanvasPageNavigationReason reason;
            if (!Enum.TryParse(json.GetStringAt(0), out reason))
            {
                return false;
            }
            this.Reason = reason;

            this.Item = CanvasItemManager.Current.GetItemByInternalName(json.GetStringAt(1));

            return true;
        }
    }
}
