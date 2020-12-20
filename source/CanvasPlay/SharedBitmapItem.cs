using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading;
using Windows.Graphics.Imaging;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  共有で渡される登録候補の画像アイテム.
    /// </summary>
    class SharedBitmapItem : ISharedItem
    {
        /// <summary>
        ///  共有の<c>DataPackageView</c>からこのクラスのインスタンスを作成する.
        /// </summary>
        /// <param name="package">共有の<c>DataPackageView</c></param>
        /// <returns>このクラスのインスタンス</returns>
        public static async Task<SharedBitmapItem> CreateFromDataPackage(DataPackageView package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (!package.Contains(StandardDataFormats.Bitmap))
            {
                return null;
            }

            var bmpStream = await package.GetBitmapAsync();
            using (var bmpStreamWithContentType = await bmpStream.OpenReadAsync())
            {
                var bmpImage = new BitmapImage();
                bmpImage.SetSource(bmpStreamWithContentType);

                var item = new SharedBitmapItem(bmpImage);
                await item.KeepSharedBitmapInfoAsync(bmpStreamWithContentType);
                return item;
            }
        }

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="image">共有したBitmap</param>
        public SharedBitmapItem(BitmapImage image)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            this.Image = image;
        }

        /// <summary>
        ///  共有したBitmap.
        /// </summary>
        public BitmapImage Image { get; private set; }

        /// <summary>
        ///  ロック画面登録用のファイル.
        /// </summary>
        public StorageFile File { get; private set; }

        /// <summary>
        ///  一時ファイルとして扱うかどうか.
        /// </summary>
        public bool IsTemporary { get { return true; } }

        /// <summary>
        ///  ロック画面登録用のファイルを準備する.
        /// </summary>
        /// <returns></returns>
        public async Task PrepareFileAsync()
        {
            if (this.imageStream == null)
            {
                return;
            }
            if (String.IsNullOrEmpty(this.fileName))
            {
                return;
            }

            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(@"SharedImageCache", CreationCollisionOption.OpenIfExists);
            if (folder == null)
            {
                return;
            }
            var file = await folder.CreateFileAsync(this.fileName, CreationCollisionOption.GenerateUniqueName);
            if (file == null)
            {
                return;
            }

            using (var strm = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                strm.Seek(0);
                strm.Size = 0;
                this.imageStream.Seek(0);
                await Windows.Storage.Streams.RandomAccessStream.CopyAsync(this.imageStream, strm);
            }
            this.File = file;
        }

        /// <summary>
        ///  ロック画面に設定する.
        /// </summary>
        /// <returns></returns>
        public async Task SetLockscreenAsync()
        {
            if (this.imageStream == null)
            {
                return;
            }

            await Windows.System.UserProfile.LockScreen.SetImageStreamAsync(this.imageStream);
        }


        /// <summary>
        ///  共有Bitmapのstream.
        /// </summary>
        private Windows.Storage.Streams.InMemoryRandomAccessStream imageStream;

        /// <summary>
        ///  共有Bitmapをファイルに保存するときに使うファイル名.
        /// </summary>
        private String fileName;

        /// <summary>
        ///  あとでファイルとして保存できるように共有Bitmapの情報を保存しておく.
        /// </summary>
        /// <param name="inStream"></param>
        /// <returns></returns>
        private async Task KeepSharedBitmapInfoAsync(Windows.Storage.Streams.IRandomAccessStreamWithContentType inStream)
        {
            //var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(inStream);

            //this.fileName = DateTimeOffset.UtcNow.Ticks.ToString() + decoder.DecoderInformation.FileExtensions[0];
            //this.imageStream = new InMemoryRandomAccessStream();
            //inStream.Seek(0);
            //await Windows.Storage.Streams.RandomAccessStream.CopyAsync(inStream, imageStream);

            this.imageStream = await ConvertPngStreamAsync(inStream);
            this.fileName = DateTimeOffset.UtcNow.Ticks.ToString() + ".png";
        }

        /// <summary>
        ///  画像データのストリームをpng形式に変換する.
        /// </summary>
        /// <param name="inStream"></param>
        /// <returns></returns>
        private async Task<InMemoryRandomAccessStream> ConvertPngStreamAsync(IRandomAccessStreamWithContentType inStream)
        {
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(inStream);
            var pixelDataProvider = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Premultiplied,
                new BitmapTransform(),
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);
            var pixelData = pixelDataProvider.DetachPixelData();

            var outStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Ignore,
                decoder.OrientedPixelWidth,
                decoder.OrientedPixelHeight,
                decoder.DpiX,
                decoder.DpiY,
                pixelData);
            await encoder.FlushAsync();

            return outStream;
        }
    }
}
