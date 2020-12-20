using MyApps.CanvasPlay.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;

// 共有ターゲット コントラクトのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234241 を参照してください

namespace MyApps.CanvasPlay
{
    /// <summary>
    /// このページを使用すると、他のアプリケーションがこのアプリケーションを介してコンテンツを共有できます。
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        /// <summary>
        /// 共有操作について、Windows と通信するためのチャネルを提供します。
        /// </summary>
        private Windows.ApplicationModel.DataTransfer.ShareTarget.ShareOperation _shareOperation;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// これは厳密に型指定されたビュー モデルに変更できます。
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        public ShareTargetPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 他のアプリケーションがこのアプリケーションを介してコンテンツの共有を求めた場合に呼び出されます。
        /// </summary>
        /// <param name="e">Windows と連携して処理するために使用されるアクティベーション データ。</param>
        public async void Activate(ShareTargetActivatedEventArgs e)
        {
            this._shareOperation = e.ShareOperation;

            // ビュー モデルを使用して、共有されるコンテンツのメタデータを通信します
            var shareProperties = this._shareOperation.Data.Properties;
            this.DefaultViewModel["Title"] = shareProperties.Title;
            this.DefaultViewModel["Description"] = shareProperties.Description;
            this.DefaultViewModel["Sharing"] = false;
            this.defaultViewModel["LoadingItem"] = true;
            this.defaultViewModel["EditButtonsVisibility"] = Visibility.Collapsed;
            Window.Current.Content = this;
            Window.Current.Activate();

            await AddSharedItemsAsync(this._shareOperation);
            this.defaultViewModel["EditButtonsVisibility"] = Visibility.Visible;
            this.defaultViewModel["LoadingItem"] = false;
        }

        /// <summary>
        /// ユーザーが [共有] をクリックしたときに呼び出されます。
        /// </summary>
        /// <param name="sender">共有を開始するときに使用される Button インスタンス。</param>
        /// <param name="e">ボタンがどのようにクリックされたかを説明するイベント データ。</param>
        private async void ShareButton_Click(object sender, RoutedEventArgs e)
        {
            this.DefaultViewModel["Sharing"] = true;
            this._shareOperation.ReportStarted();

            // TODO: this._shareOperation.Data を使用して共有シナリオに適した
            //       作業を実行します。通常は、カスタム ユーザー インターフェイス要素を介して
            //       このページに追加されたカスタム ユーザー インターフェイス要素を介して
            //       this.DefaultViewModel["Comment"]

            this.mainCanvas.MergeNewTraces();
            await this.mainCanvas.Item.SaveAsync();
            if (!CanvasItemManager.Current.Items.Contains(this.mainCanvas.Item))
            {
                await CanvasItemManager.Current.AddItemAsync(this.mainCanvas.Item);
            }

            this._shareOperation.ReportCompleted();
        }

        /// <summary>
        ///  "取り消し"ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.mainCanvas.CancelNewTraces();
            this.mainCanvas.ClearCanvas();

            if (this.sharedBitmapItem != null)
            {
                await this.mainCanvas.SetImageAsync(this.sharedBitmapItem.File);
            }
        }

        /// <summary>
        ///  共有データに含まれる画像とファイルを登録候補として追加する.
        /// </summary>
        /// <param name="shareOperation"></param>
        private async Task AddSharedItemsAsync(ShareOperation shareOperation)
        {
            var bmpItem = await SharedBitmapItem.CreateFromDataPackage(shareOperation.Data);
            if (bmpItem == null)
            {
                return;
            }

            this.mainCanvas.Item = new CanvasItem();
            this.mainCanvas.IsEditing = true;

            if (this.sharedItemCache == null)
            {
                this.sharedItemCache = new CameraCache("SharedItemCache");
                await this.sharedItemCache.ClearAsync();
            }

            await bmpItem.PrepareFileAsync();
            await this.sharedItemCache.MoveAndAddAsync(bmpItem.File);

            await this.mainCanvas.SetImageAsync(bmpItem.File, true);
            this.sharedBitmapItem = bmpItem;
        }

        /// <summary>
        ///  共有データの一時的管理.
        /// </summary>
        private CameraCache sharedItemCache;
        private SharedBitmapItem sharedBitmapItem;

        /// <summary>
        ///  "線の色"ポップアップが表示されるときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void strokeColorPickerFlyout_Opened(object sender, object e)
        {
            this.strokeColorPicker.Color = new ColorValue(this.mainCanvas.StrokeColor);
        }

    }
}
