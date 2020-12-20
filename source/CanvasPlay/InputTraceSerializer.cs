using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  手描き入力の保存、読み込みを行う.
    /// </summary>
    class InputTraceSerializer
    {
        /// <summary>
        ///  保存先のフォルダー.
        /// </summary>
        public StorageFolder TargetFolder { get; set; }

        /// <summary>
        ///  保存を行う.
        /// </summary>
        /// <param name="traceRecorder"></param>
        /// <returns></returns>
        public async Task SerializeAsync(InputTraceRecorder traceRecorder)
        {
            await Task.Run(async () =>
            {
                var file = await this.TargetFolder.CreateFileAsync("play.log", CreationCollisionOption.ReplaceExisting);
                if (file == null)
                {
                    return;
                }
                using (var stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (var writer = new DataWriter(stream))
                    {
                        writer.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf16LE;

                        JsonArray jsonArray = new JsonArray();
                        foreach (var trace in traceRecorder.Traces)
                        {
                            if (IsTraceWithFile(trace))
                            {
                                if (!await SaveFileForTraceAsync(trace as IInputTraceWithFile))
                                {
                                    continue;
                                }
                            }
                            jsonArray.Clear();
                            trace.SaveAsJson(jsonArray);
                            writer.WriteString(jsonArray.Stringify());
                            writer.WriteString(Environment.NewLine);
                        }

                        await writer.StoreAsync();
                        await writer.FlushAsync();
                    }
                }
            });
        }

        /// <summary>
        ///  読み込みを行う.
        /// </summary>
        /// <returns></returns>
        public async Task<List<InputTraceBase>> DeserializeAsync()
        {
            return await Task.Run(async () =>
            {
                var file = (await this.TargetFolder.TryGetItemAsync("play.log")) as StorageFile;
                if (file == null)
                {
                    return null;
                }

                var texts = await FileIO.ReadLinesAsync(file, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
                if (texts.Count == 0)
                {
                    return null;
                }

                var traces = new List<InputTraceBase>(texts.Count);
                foreach (var text in texts)
                {
                    var trace = ParseTrace(text);
                    if (trace == null)
                    {
                        continue;
                    }

                    if (IsTraceWithFile(trace))
                    {
                        await LoadFileForTraceAsync(trace as IInputTraceWithFile);
                    }

                    traces.Add(trace);
                }

                return traces;
            });
        }

        /// <summary>
        ///  文字列にシリアライズされた手描き入力を復元する.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static InputTraceBase ParseTrace(string text)
        {
            JsonArray jsonArray;
            if (!JsonArray.TryParse(text, out jsonArray))
            {
                return null;
            }

            InputTraceKind kind;
            if (!Enum.TryParse(jsonArray.GetStringAt(0), out kind))
            {
                return null;
            }

            InputTraceBase trace;
            switch (kind)
            {
                case InputTraceKind.BeginStroke:
                    trace = new BeginStrokeTrace();
                    break;
                case InputTraceKind.MoveStroke:
                    trace = new MoveStrokeTrace();
                    break;
                case InputTraceKind.EndStroke:
                    trace = new EndStrokeTrace();
                    break;
                case InputTraceKind.RemoveStroke:
                    trace = new RemoveStrokeTrace();
                    break;
                case InputTraceKind.SetStrokeColor:
                    trace = new SetStrokeColorTrace();
                    break;
                case InputTraceKind.SetStrokeThickness:
                    trace = new SetStrokeThicknessTrace();
                    break;
                case InputTraceKind.SetBackgroundColor:
                    trace = new SetBackgroundColorTrace();
                    break;
                case InputTraceKind.SetImage:
                    trace = new SetImageTrace();
                    break;

                default:
                    return null;
            }

            trace.LoadFromJson(jsonArray);
            return trace;
        }

        private static bool IsTraceWithFile(InputTraceBase trace)
        {
            System.Diagnostics.Debug.Assert(trace != null);

            return trace.Kind == InputTraceKind.SetImage;
        }

        private async Task<bool> SaveFileForTraceAsync(IInputTraceWithFile trace)
        {
            System.Diagnostics.Debug.Assert(trace != null);

            if (trace.Source == InputTraceFileSource.CreateNew)
            {
                await trace.File.MoveAsync(this.TargetFolder, trace.FileName, NameCollisionOption.GenerateUniqueName);
            }
            trace.FileName = trace.File.Name;

            return true;
        }

        private async Task LoadFileForTraceAsync(IInputTraceWithFile trace)
        {
            System.Diagnostics.Debug.Assert(trace != null);

            trace.File = await this.TargetFolder.GetFileAsync(trace.FileName);
            if (trace.File == null)
            {
                return;
            }

            trace.Source = InputTraceFileSource.LoadFromTrace;
        }
    }
}
