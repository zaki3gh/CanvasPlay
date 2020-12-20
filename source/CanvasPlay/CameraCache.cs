using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  カメラで撮影したファイルの管理.
    /// </summary>
    class CameraCache
    {
        /// <summary>
        ///  作業用フォルダーの名前.
        /// </summary>
        private readonly string cacheFolderName = String.Empty;

        public CameraCache(string cacheFolderName)
        {
            if (String.IsNullOrEmpty(cacheFolderName))
            {
                throw new ArgumentNullException("cacheFolderName");
            }
            this.cacheFolderName = cacheFolderName;
        }

        /// <summary>
        ///  管理対象に追加する.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task MoveAndAddAsync(StorageFile file)
        {
            System.Diagnostics.Debug.Assert(file != null);

            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(this.cacheFolderName, CreationCollisionOption.OpenIfExists);
            if (folder == null)
            {
                return;
            }

            await file.MoveAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        ///  管理対象に追加する.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<StorageFile> CopyAndAddAsync(StorageFile file)
        {
            System.Diagnostics.Debug.Assert(file != null);

            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(this.cacheFolderName, CreationCollisionOption.OpenIfExists);
            if (folder == null)
            {
                return null;
            }

            return await file.CopyAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        ///  管理対象として新しいファイルを追加する.
        /// </summary>
        /// <param name="desiredNewName"></param>
        /// <returns></returns>
        public async Task<StorageFile> CreateNewAsync(string desiredNewName)
        {
            System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(desiredNewName));

            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(this.cacheFolderName, CreationCollisionOption.OpenIfExists);
            if (folder == null)
            {
                return null;
            }

            return await folder.CreateFileAsync(desiredNewName, CreationCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        ///  管理対象のファイルをすべて削除する.
        /// </summary>
        /// <returns></returns>
        public async Task ClearAsync()
        {
            try
            {
                var folder = await ApplicationData.Current.TemporaryFolder.GetFolderAsync(this.cacheFolderName);
                if (folder == null)
                {
                    return;
                }

                await folder.DeleteAsync();
            }
            catch (System.IO.FileNotFoundException)
            {
                return;
            }
        }
    }
}
