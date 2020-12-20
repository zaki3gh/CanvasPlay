using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Storage;
using Windows.Storage.Streams;

namespace MyApps.CanvasPlay
{
    static class ZipArchiveHelper
    {
        /// <summary>
        ///  フォルダーをZip圧縮する.
        /// </summary>
        /// <param name="inFolder">Zip圧縮するフォルダー</param>
        /// <param name="outFile">出力先のZipファイル</param>
        /// <returns></returns>
        public static async Task CreateFromDirectoryAsync(StorageFolder inFolder, StorageFile outFile)
        {
            if (inFolder == null)
            {
                throw new ArgumentNullException("inFolder");
            }
            if (outFile == null)
            {
                throw new ArgumentNullException("outFile");
            }

            using (var outStream = await outFile.OpenStreamForWriteAsync())
            {
                using (var zipArchive = new ZipArchive(outStream, ZipArchiveMode.Create))
                {
                    await AddEntriesFromFolderAsync(zipArchive, inFolder);
                }

                outStream.ReadByte();
            }
        }

        /// <summary>
        ///  ファイルから<c>ZipArchive</c>を作成する.
        /// </summary>
        /// <param name="file">ファイル</param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static async Task<ZipArchive> CreateZipArchiveFromFileAsync(StorageFile file, ZipArchiveMode mode)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            var stream = await file.OpenStreamForWriteAsync();
            if (mode == ZipArchiveMode.Create)
            {
                stream.SetLength(0);
            }
            return new ZipArchive(stream, mode);
        }

        /// <summary>
        ///  フォルダーをzipに追加する.
        /// </summary>
        /// <param name="zip">zip</param>
        /// <param name="folder">追加するフォルダー</param>
        /// <returns></returns>
        public static async Task AddEntriesFromFolderAsync(ZipArchive zip, StorageFolder folder)
        {
            if (zip == null)
            {
                throw new ArgumentNullException("zip");
            }
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            var inFiles = await folder.GetFilesAsync();
            foreach (var inFile in inFiles)
            {
                var entry = zip.CreateEntry(String.Format(@"{0}\{1}", folder.Name, inFile.Name));
                using (var entryStream = entry.Open())
                using (var inFileStream = await inFile.OpenStreamForReadAsync())
                {
                    await inFileStream.CopyToAsync(entryStream);
                }
            }
        }

        /// <summary>
        ///  テキストデータをZipに追加する.
        /// </summary>
        /// <param name="zip"></param>
        /// <param name="entryName">エントリ名</param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static async Task AddEntriesFromTextAsync(ZipArchive zip, string entryName, string text, System.Text.Encoding encoding)
        {
            if (zip == null)
            {
                throw new ArgumentNullException("zip");
            }
            if (String.IsNullOrEmpty(entryName))
            {
                throw new ArgumentNullException("entryName");
            }

            await Task.Run(()=>{
                var entry = zip.CreateEntry(entryName);
                using (var entryStream = entry.Open())
                {
                    using (var writer = new StreamWriter(entryStream, encoding))
                    {
                        writer.Write(text);
                    }
                }
            });
        }

        /// <summary>
        ///  zipファイルを指定されたフォルダー配下に展開する.
        /// </summary>
        /// <param name="zip">zipファイル</param>
        /// <param name="outFolder">展開先</param>
        /// <returns></returns>
        public static async Task ExtractFromFileAsync(ZipArchive zip, StorageFolder outFolder)
        {
            if (zip == null)
            {
                throw new ArgumentNullException("zip");
            }
            if (outFolder == null)
            {
                throw new ArgumentNullException("outFolder");
            }

            foreach (var entry in zip.Entries)
            {
                var outFile = await outFolder.CreateFileAsync(entry.FullName, CreationCollisionOption.ReplaceExisting);

                using (var entryStream = entry.Open())
                using (var outFileStream = await outFile.OpenStreamForWriteAsync())
                {
                    await entryStream.CopyToAsync(outFileStream);
                }
            }
        }

    }
}
