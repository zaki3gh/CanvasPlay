using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;
using Windows.Storage;


namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  手描き入力のキャンバスを表すアイテム.
    /// </summary>
    public class CanvasItem :
        System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public CanvasItem()
        {
            this.InputRecorder = new InputTraceRecorder();
            this.InternalName = String.Empty;
            this.DisplayName = String.Empty;
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        public CanvasItem(String internalName)
        {
            if (String.IsNullOrEmpty(internalName))
            {
                throw new ArgumentException("internalName");
            }

            this.InputRecorder = new InputTraceRecorder();
            this.InternalName = internalName;
            this.DisplayName = String.Empty;
        }

        /// <summary>
        ///  ファイルとして保存する.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SaveAsync()
        {
            StorageFolder folder;
            if (!String.IsNullOrEmpty(this.InternalName))
            {
                folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(this.InternalName);
            }
            else
            {
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(DateTime.UtcNow.Ticks.ToString(), CreationCollisionOption.GenerateUniqueName);
                if (folder != null)
                {
                    this.InternalName = folder.Name;
                    this.DisplayName = DateTime.Now.ToString();
                }
            }
            if (folder == null)
            {
                return false;
            }

            var serializer = new InputTraceSerializer()
            {
                TargetFolder = folder,
            };

            await serializer.SerializeAsync(this.InputRecorder);
            return true;
        }

        /// <summary>
        ///  ファイルから読み込む.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> LoadAsync()
        {
            this.InputRecorder.Clear();
            if (String.IsNullOrEmpty(this.InternalName))
            {
                return true;
            }

            try
            {
                var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(this.InternalName);
                var serializer = new InputTraceSerializer()
                {
                    TargetFolder = folder,
                };
                var traces = await serializer.DeserializeAsync();
                if (traces == null)
                {
                    return false;
                }
                this.InputRecorder.SetTraces(traces);
                return true;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                return false;
            }
        }

        /// <summary>
        ///  ファイルを削除する.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RemoveAsync()
        {
            if (String.IsNullOrEmpty(this.InternalName))
            {
                throw new InvalidOperationException();
            }

            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(this.InternalName);
            if (folder == null)
            {
                // 削除済みということ
                return true;
            }

            await folder.DeleteAsync();
            return true;
        }

        /// <summary>
        ///  手描き入力の記録.
        /// </summary>
        public InputTraceRecorder InputRecorder { get; private set; }

        /// <summary>
        ///  表示名.
        /// </summary>
        public string DisplayName 
        {
            get { return this.displayName; }
            set
            {
                this.displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }
        /// <summary>DisplayNameプロパティ</summary>
        private string displayName;

        /// <summary>
        ///  内部名.
        /// </summary>
        public string InternalName 
        {
            get { return this.internalName; }
            private set 
            {
                this.internalName = value;
                OnPropertyChanged("InternalName");
            }
        }
        /// <summary>InternalNameプロパティ</summary>
        private string internalName;

        #region INotifyPropertyChanged

        /// <summary>
        ///  INotifyPropertyChanged.PropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///  PropertyChangedを発生させる.
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        protected void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region import / export

        /// <summary>
        ///  手描き入力を外部に保存する.
        /// </summary>
        /// <param name="targetArchive">保存先のZipArchive</param>
        /// <returns></returns>
        public async Task ExportAsync(System.IO.Compression.ZipArchive targetArchive)
        {
            if (targetArchive == null)
            {
                throw new ArgumentNullException("targetArchive");
            }

            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync(this.InternalName);
            await ZipArchiveHelper.AddEntriesFromFolderAsync(targetArchive, folder);

            var propertyText = SerializeProperties();
            await ZipArchiveHelper.AddEntriesFromTextAsync(targetArchive, String.Format(PropertyEntryNameFormat, this.InternalName), propertyText, Encoding.UTF8);
        }

        /// <summary>
        ///  外部に保存した手描き入力を読み込む.
        /// </summary>
        /// <param name="sourceArchive"></param>
        /// <returns></returns>
        public static async Task ImportAsync(System.IO.Compression.ZipArchive sourceArchive)
        {
            if (sourceArchive == null)
            {
                throw new ArgumentNullException("sourceArchive");
            }

            var workdir = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("ItemImport", CreationCollisionOption.ReplaceExisting);
            await ZipArchiveHelper.ExtractFromFileAsync(sourceArchive, workdir);

            var itemsDir = await workdir.GetFoldersAsync();
            foreach (var itemDir in itemsDir)
            {
                var item = new CanvasItem();
                if (!await item.SaveAsync())
                {
                    continue;
                }

                var propertyFile = (await workdir.TryGetItemAsync(String.Format(PropertyEntryNameFormat, itemDir.Name))) as StorageFile;
                if (propertyFile == null)
                {
                    continue;
                }
                var propertyText = await FileIO.ReadTextAsync(propertyFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                item.DeserializeProperties(propertyText);
                await propertyFile.DeleteAsync();

                var newItemDir = await ApplicationData.Current.LocalFolder.GetFolderAsync(item.InternalName);
                foreach (var file in await itemDir.GetFilesAsync())
                {
                    await file.MoveAsync(newItemDir, file.Name, NameCollisionOption.ReplaceExisting);
                }

                if (!await item.LoadAsync())
                {
                    continue;
                }
                await CanvasItemManager.Current.AddItemAsync(item);
            }
            await workdir.DeleteAsync();
        }

        /// <summary>
        ///  この手描き入力のプロパティを文字列にシリアライズする.
        /// </summary>
        /// <returns></returns>
        private string SerializeProperties()
        {
            var json = new JsonObject();
            json.Add(PropertyKeyDisplayName, JsonValue.CreateStringValue(this.DisplayName));
            return json.Stringify();
        }

        /// <summary>
        ///  この手描き入力のプロパティを文字列からにデシリアライズする.
        /// </summary>
        /// <returns></returns>
        private bool DeserializeProperties(string text)
        {
            JsonObject json;
            if (!JsonObject.TryParse(text, out json))
            {
                return false;
            }

            this.DisplayName = json.GetNamedString(PropertyKeyDisplayName, String.Empty);
            return false;
        }

        /// <summary>
        ///  この手描き入力のプロパティのzipのエントリ名フォーマット.
        /// </summary>
        private const string PropertyEntryNameFormat = @"{0}\property";

        /// <summary>
        ///  プロパティ名 - 表示名.
        /// </summary>
        private const string PropertyKeyDisplayName = "displayName";

        #endregion
    }
}
