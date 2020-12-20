using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;


namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  共有で渡される登録候補のアイテムの共通インターフェース.
    /// </summary>
    interface ISharedItem
    {
        /// <summary>共有ページで確認するためのサムネイル画像</summary>
        BitmapImage Image { get; }

        /// <summary>
        ///  ロック画面登録用のファイル.
        /// </summary>
        StorageFile File { get; }

        /// <summary>
        ///  一時ファイルとして扱うかどうか.
        /// </summary>
        bool IsTemporary { get; }

        /// <summary>
        ///  ロック画面登録用のファイルを準備する.
        /// </summary>
        /// <returns></returns>
        Task PrepareFileAsync();

        /// <summary>
        ///  ロック画面に設定する.
        /// </summary>
        /// <returns></returns>
        Task SetLockscreenAsync();
    }
}
