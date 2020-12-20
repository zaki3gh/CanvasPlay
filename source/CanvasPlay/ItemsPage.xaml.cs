using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// アイテム ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234233 を参照してください

namespace MyApps.CanvasPlay
{
    /// <summary>
    /// アイテムのコレクションのプレビューを表示するページです。このページは、分割アプリケーションで使用できる
    /// グループを表示し、その 1 つを選択するために使用されます。
    /// </summary>
    public sealed partial class ItemsPage : MyApps.CanvasPlay.Common.LayoutAwarePage
    {
        /// <summary>
        ///  Static Constructor.
        /// </summary>
        static ItemsPage()
        {
            InitProperties();
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        public ItemsPage()
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
            //// TODO: 問題のドメインでサンプル データを置き換えるのに適したデータ モデルを作成します
            //var sampleDataGroups = SampleDataSource.GetGroups((String)navigationParameter);
            //this.DefaultViewModel["Items"] = sampleDataGroups;

            this.DefaultViewModel["Items"] = CanvasItemManager.Current.Items;
            if (CanvasItemManager.Current.Items.Count == 0)
            {
                this.ItemsChanging = true;
                await CanvasItemManager.Current.LoadAllItemsAsync();
                this.ItemsChanging = false;
            }

            // 一つも手描き入力がない場合には新規作成に移動する
            if (CanvasItemManager.Current.Items.Count == 0)
            {
                NavigateToItemPage(CanvasPageNavigationReason.New, null);
            }
        }

        /// <summary>
        ///  現在表示されている<c>GridView</c>か<c>ListView</c>を取得する.
        /// </summary>
        /// <returns></returns>
        private ListViewBase GetItemsView()
        {
            if (this.itemGridView.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return this.itemGridView;
            }
            else if (this.itemListView.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return this.itemListView;
            }

            return null;
        }

        /// <summary>
        ///  現在選択されているアイテムを取得する.
        /// </summary>
        /// <returns></returns>
        private CanvasItem GetSelectedItem()
        {
            var itemsView = GetItemsView();
            if (itemsView == null)
            {
                return null;
            }

            var item = itemsView.SelectedItem as CanvasItem;
            if (item == null)
            {
                return null;
            }

            return item;
        }

        /// <summary>
        ///  現在選択されているアイテムを取得する.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<CanvasItem> GetSelectedItems()
        {
            var itemsView = GetItemsView();
            if (itemsView == null)
            {
                return null;
            }

            return itemsView.SelectedItems.Cast<CanvasItem>();
        }

        /// <summary>
        ///  アイテム一覧の<c>GridView</c>または<c>ListView</c>の選択されているアイテムが変わったイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemsView = GetItemsView();
            if (sender != itemsView)
            {
                return;
            }

            this.ItemsSelected = (itemsView.SelectedItem != null);
        }

        /// <summary>
        ///  依存プロパティを初期化する.
        /// </summary>
        private static void InitProperties()
        {
            ItemsSelectedProperty = DependencyProperty.Register("ItemsSelected", typeof(bool), typeof(ItemsPage), null);
            ItemsChangingProperty = DependencyProperty.Register("ItemsChanging", typeof(bool), typeof(ItemsPage), null);
        }

        /// <summary>
        ///  <c>ItemsSelected</c>プロパティ.
        /// </summary>
        public static DependencyProperty ItemsSelectedProperty { get; private set; }

        /// <summary>
        ///  アイテムが選択されている時に<c>true</c>になるプロパティ.
        /// </summary>
        public bool ItemsSelected
        {
            get { return (bool)GetValue(ItemsSelectedProperty); }
            set { SetValue(ItemsSelectedProperty, value); }
        }

        /// <summary>
        ///  <c>ItemsChanging</c>プロパティ.
        /// </summary>
        public static DependencyProperty ItemsChangingProperty { get; private set; }

        /// <summary>
        ///  アイテムが変更されている時に<c>true</c>になるプロパティ.
        /// </summary>
        public bool ItemsChanging
        {
            get { return (bool)GetValue(ItemsChangingProperty); }
            set { SetValue(ItemsChangingProperty, value); }
        }

        /// <summary>
        ///  アイテム用のページに移動する.
        /// </summary>
        /// <param name="reason">理由</param>
        /// <param name="item">ページに表示する手描きアイテム</param>
        private void NavigateToItemPage(CanvasPageNavigationReason reason, CanvasItem item)
        {
            var nav = new CanvasPageNavigation()
            {
                Reason = reason, 
                Item = item,
            };
            this.Frame.Navigate(typeof(CanvasPage), nav.Serialize());
        }

        /// <summary>
        ///  選択されたアイテムを削除する.
        /// </summary>
        private async void DeleteSelectedItemsAsync()
        {
            var itemsView = GetItemsView();
            if ((itemsView == null) || (itemsView.SelectedItems.Count == 0))
            {
                return;
            }

            this.ItemsChanging = true;
            var removeItems = new List<CanvasItem>(itemsView.SelectedItems.Cast<CanvasItem>());
            foreach (var item in removeItems)
            {
                await CanvasItemManager.Current.RemoveItemAsync(item);
            }
            this.ItemsChanging = false;
        }

        /// <summary>
        ///  選択されたアイテムを外部ファイルに保存する.
        /// </summary>
        /// <returns></returns>
        private async Task ExportItemsAsync()
        {
            var items = GetSelectedItems();
            if (items == null)
            {
                return;
            }

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.FileTypeChoices.Add("zip", new string[] { ".zip" });
            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return;
            }

            this.ItemsChanging = true;
            using (var zipArchive = await ZipArchiveHelper.CreateZipArchiveFromFileAsync(file, System.IO.Compression.ZipArchiveMode.Create))
            {
                foreach (var item in items)
                {
                    await item.ExportAsync(zipArchive);
                }
            }
            this.ItemsChanging = false;
        }

        /// <summary>
        /// 外部ファイルに保存されたアイテムを読み込む.
        /// </summary>
        /// <returns></returns>
        private async Task ImportItemsAsync()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".zip");
            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            this.ItemsChanging = true;
            using (var zipArchive = await ZipArchiveHelper.CreateZipArchiveFromFileAsync(file, System.IO.Compression.ZipArchiveMode.Read))
            {
                await CanvasItem.ImportAsync(zipArchive);
            }
            this.ItemsChanging = false;
        }

        /// <summary>
        /// アイテムがクリックされたときに呼び出されます。
        /// </summary>
        /// <param name="sender">クリックされたアイテムを表示する GridView (アプリケーションがスナップ
        /// されている場合は ListView) です。</param>
        /// <param name="e">クリックされたアイテムを説明するイベント データ。</param>
        void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            NavigateToItemPage(CanvasPageNavigationReason.Play, e.ClickedItem as CanvasItem);
        }

        /// <summary>
        ///  "新規"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToItemPage(CanvasPageNavigationReason.New, null);
        }

        /// <summary>
        ///  "再生"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToItemPage(CanvasPageNavigationReason.Play, GetSelectedItem());
        }

        /// <summary>
        ///  "編集"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToItemPage(CanvasPageNavigationReason.Edit, GetSelectedItem());
        }

        /// <summary>
        ///  "削除"ボタンがクリックされたときの処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItemsAsync();
        }

        /// <summary>
        ///  "ファイルに保存する"ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            await ExportItemsAsync();
        }

        /// <summary>
        ///  "ファイルから読み込む"ボタンがクリックされた時の処理.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            await ImportItemsAsync();
        }

        /// <summary>
        ///  AppBarの<c>Loaded</c>イベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BottomAppBar_Loaded(object sender, RoutedEventArgs e)
        {
            //var panel = (sender as AppBar).Content as Panel;
            //foreach (var ui in panel.Children)
            //{
            //    base.StartLayoutUpdates(ui, new RoutedEventArgs());
            //}

            (sender as AppBar).DataContext = this;
        }

        ///// <summary>
        /////  AppBarの<c>Unloaded</c>イベントを処理する.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void BottomAppBar_Unloaded(object sender, RoutedEventArgs e)
        //{
        //    var panel = (sender as AppBar).Content as Panel;
        //    foreach (var ui in panel.Children)
        //    {
        //        base.StopLayoutUpdates(ui, new RoutedEventArgs());
        //    }
        //}

    }
}
