using MyApps.CanvasPlay.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 分割アプリケーション テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234228 を参照してください

namespace MyApps.CanvasPlay
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        /// 最初の行であり、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.Resuming += OnResuming;
        }

        /// <summary>
        /// アプリケーションがエンド ユーザーによって正常に起動されたときに呼び出されます。他のエントリ ポイントは、
        /// アプリケーションが特定のファイルを開くために呼び出されたときに
        /// 検索結果やその他の情報を表示するために使用されます。
        /// </summary>
        /// <param name="args">起動要求とプロセスの詳細を表示します。</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // ウィンドウに既にコンテンツが表示されている場合は、アプリケーションの初期化を繰り返さずに、
            // ウィンドウがアクティブであることだけを確認してください
            
            if (rootFrame == null)
            {
                // ナビゲーション コンテキストとして動作するフレームを作成し、最初のページに移動します
                rootFrame = new Frame();
                //フレームを SuspensionManager キーに関連付けます                                
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // 必要な場合のみ、保存されたセッション状態を復元します
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        //状態の復元に何か問題があります。
                        //状態がないものとして続行します
                    }
                }

                // フレームを現在のウィンドウに配置します
                Window.Current.Content = rootFrame;

                this.OnLaunchedDispatcher = rootFrame.Dispatcher;
            }
            if (rootFrame.Content == null)
            {
                // ナビゲーション スタックが復元されていない場合、最初のページに移動します。
                // このとき、必要な情報をナビゲーション パラメーターとして渡して、新しいページを
                // を構成します
                if (!rootFrame.Navigate(typeof(ItemsPage), "AllGroups"))
//                if (!rootFrame.Navigate(typeof(CanvasPage), "AllGroups"))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // 現在のウィンドウがアクティブであることを確認します
            Window.Current.Activate();
        }

        /// <summary>
        /// アプリケーションの実行が中断されたときに呼び出されます。アプリケーションの状態は、
        /// アプリケーションが終了されるのか、メモリの内容がそのままで再開されるのか
        /// わからない状態で保存されます。
        /// </summary>
        /// <param name="sender">中断要求の送信元。</param>
        /// <param name="e">中断要求の詳細。</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        ///  アプリケーションの実行が再開されるときの処理を行う.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnResuming(object sender, object e)
        {
            // 手描き入力アイテムページを表示中だったのであれば
            // 状態を回復し再生させる

            if (Window.Current == null)
            {
                return;
            }
            var frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }
            var page = frame.Content as CanvasPage;
            if (page == null)
            {
                return;
            }

            page.OnResuming();
        }

        /// <summary>
        ///  現在実行中のアプリインスタンス.
        /// </summary>
        public static App CurrentApp
        {
            get { return App.Current as App; }
        }

        /// <summary>
        ///  アプリケーションの文字列リソース.
        /// </summary>
        internal MyStringResources MyResources
        {
            get { return this.Resources["MyResource"] as MyStringResources; }
        }

        /// <summary>
        ///  メイン画面に関連付けられている<c>Dispatcher</c>.
        /// </summary>
        internal Windows.UI.Core.CoreDispatcher OnLaunchedDispatcher { get; private set; }

        /// <summary>
        /// Invoked when the application is activated as the target of a sharing operation.
        /// </summary>
        /// <param name="e">アクティブ化要求に関する詳細を表示します。</param>
        protected override void OnShareTargetActivated(Windows.ApplicationModel.Activation.ShareTargetActivatedEventArgs e)
        {
            var shareTargetPage = new MyApps.CanvasPlay.ShareTargetPage();
            shareTargetPage.Activate(e);
        }
    }
}
