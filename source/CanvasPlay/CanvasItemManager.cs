using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.Storage;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  手描き入力を管理する.
    /// </summary>
    class CanvasItemManager
    {
        /// <summary>
        ///  static constructor.
        /// </summary>
        static CanvasItemManager()
        {
            Current = new CanvasItemManager();
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        private CanvasItemManager()
        {
            this.itemsDataContainer = ApplicationData.Current.LocalSettings.CreateContainer(ItemsDataContainerName, ApplicationDataCreateDisposition.Always);

            InitPropertiesForItemsCollection();
        }

        /// <summary>
        ///  手描き入力管理のインスタンス.
        /// </summary>
        public static CanvasItemManager Current { get; private set; }

        /// <summary>
        ///  手描き入力管理の内部名を保持するApplicationDataContainerのコンテナー名.
        /// </summary>
        private const string ItemsDataContainerName = "CanvasItems";

        /// <summary>
        ///  手描き入力管理の内部名を保持するApplicationDataContainer.
        /// </summary>
        private readonly ApplicationDataContainer itemsDataContainer;

        /// <summary>
        ///  管理されている手描き入力アイテム.
        /// </summary>
        public ReadOnlyObservableCollection<CanvasItem> Items { get; private set; }

        /// <summary>
        ///  管理されている手描き入力アイテム(field).
        /// </summary>
        private readonly ObservableCollection<CanvasItem> itemsCore = new ObservableCollection<CanvasItem>();

        /// <summary>
        ///  Itemsプロパティ、itemsCoreフィールドを初期化する(constructor用).
        /// </summary>
        private void InitPropertiesForItemsCollection()
        {
            this.Items = new ReadOnlyObservableCollection<CanvasItem>(this.itemsCore);
        }

        /// <summary>
        ///  管理されている手描き入力アイテムをロードする.
        /// </summary>
        /// <returns></returns>
        public async Task LoadAllItemsAsync()
        {
            if (this.itemsDataContainer == null)
            {
                return;
            }

            List<string> removeList = null;
            foreach (var itemData in this.itemsDataContainer.Values)
            {
                var item = new CanvasItem(itemData.Key)
                {
                    DisplayName = itemData.Value as string,
                };

                var success = await item.LoadAsync();
                if (!success)
                {
                    if (removeList == null)
                    {
                        removeList = new List<string>();
                    }
                    removeList.Add(itemData.Key);
                    continue;
                }

                this.itemsCore.Add(item);
            }

            // 登録情報だけあってファイルがない物は登録解除する
            if (removeList != null)
            {
                foreach (var rm in removeList)
                {
                    this.itemsDataContainer.Values.Remove(rm);
                }
            }
        }

        /// <summary>
        ///  手描き入力アイテムを追加する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task AddItemAsync(CanvasItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if ((App.CurrentApp.OnLaunchedDispatcher == null) ||
                App.CurrentApp.OnLaunchedDispatcher.HasThreadAccess)
            {
                AddItemCore(item);
            }
            else
            {
                await App.CurrentApp.OnLaunchedDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () => { AddItemCore(item); });
            }
        }

        /// <summary>
        ///  手描き入力アイテムを追加する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private void AddItemCore(CanvasItem item)
        {
            this.itemsDataContainer.Values[item.InternalName] = item.DisplayName;
            this.itemsCore.Add(item);
        }

        /// <summary>
        ///  手描き入力アイテムを削除する.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task RemoveItemAsync(CanvasItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            await item.RemoveAsync();
            this.itemsCore.Remove(item);
            this.itemsDataContainer.Values.Remove(item.InternalName);
        }

        /// <summary>
        ///  内部名に該当する手描き入力アイテムを取得する.
        /// </summary>
        /// <param name="internalName"></param>
        /// <returns></returns>
        public CanvasItem GetItemByInternalName(string internalName)
        {
            if (String.IsNullOrEmpty(internalName))
            {
                return null;
            }

            return this.Items.FirstOrDefault(x => String.CompareOrdinal(x.InternalName, internalName) == 0);
        }
    }
}
