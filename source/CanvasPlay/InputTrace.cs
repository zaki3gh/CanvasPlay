using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Data.Json;

namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  記録されている入力の種類.
    /// </summary>
    public enum InputTraceKind
    {
        /// <summary>
        ///  手描き入力開始.
        /// </summary>
        BeginStroke,

        /// <summary>
        ///  手描き入力移動中.
        /// </summary>
        MoveStroke,

        /// <summary>
        ///  手描き入力終了.
        /// </summary>
        EndStroke,

        /// <summary>
        ///  手描き入力を削除.
        /// </summary>
        RemoveStroke,

        /// <summary>
        ///  手描き入力の色を指定する.
        /// </summary>
        SetStrokeColor,

        /// <summary>
        ///  手描き入力の幅を指定する.
        /// </summary>
        SetStrokeThickness,

        /// <summary>
        ///  背景の色を指定する.
        /// </summary>
        SetBackgroundColor,

        /// <summary>
        ///  画像を指定する.
        /// </summary>
        SetImage,
    }

    /// <summary>
    ///  記録される入力の共通基底クラス.
    /// </summary>
    public abstract class InputTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="kind">入力の種類</param>
        protected InputTraceBase(InputTraceKind kind)
        {
            this.Kind = kind;
        }

        /// <summary>
        ///  入力の種類.
        /// </summary>
        public InputTraceKind Kind { get; private set; }

        /// <summary>
        ///  入力の時刻.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        ///  文字列として保存する.
        /// </summary>
        /// <returns></returns>
        public virtual void SaveAsJson(JsonArray jsonArray)
        {
            jsonArray.Add(JsonValue.CreateStringValue(Enum.GetName(typeof(InputTraceKind), this.Kind)));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Timestamp));
        }

        /// <summary>
        ///  文字列から復元する.
        /// </summary>
        /// <param name="jsonArray"></param>
        public virtual uint LoadFromJson(JsonArray jsonArray)
        {
            this.Timestamp = (int)jsonArray.GetNumberAt(1);
            return 2;
        }
    }

    /// <summary>
    ///  手描き入力の記録の共通基底クラス.
    /// </summary>
    abstract class StrokeTraceBase : InputTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="kind">入力の種類</param>
        protected StrokeTraceBase(InputTraceKind kind)
            : base(kind)
        {
        }

        /// <summary>
        ///  手描き入力の識別番号.
        /// </summary>
        public uint Index { get; set; }

        ///// <summary>
        /////  手描き入力の開始位置.
        ///// </summary>
        //public Point Position { get; set; }

        /// <summary>
        ///  文字列として保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Index));
        }

        /// <summary>
        ///  文字列から復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            this.Index = (uint)jsonArray.GetNumberAt(index);
            return index + 1;
        }
    }

    /// <summary>
    ///  手描き入力の記録の共通基底クラス.
    /// </summary>
    abstract class StrokeTraceWithPositionBase : StrokeTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        /// <param name="kind">入力の種類</param>
        protected StrokeTraceWithPositionBase(InputTraceKind kind)
            : base(kind)
        {
        }

        /// <summary>
        ///  手描き入力の開始位置.
        /// </summary>
        public Point Position { get; set; }

        /// <summary>
        ///  文字列として保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateNumberValue(Math.Round(this.Position.X, 1)));
            jsonArray.Add(JsonValue.CreateNumberValue(Math.Round(this.Position.Y, 1)));
        }

        /// <summary>
        ///  文字列から復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            this.Position = new Point(jsonArray.GetNumberAt(index), jsonArray.GetNumberAt(index + 1));
            return index + 2;
        }
    }

    /// <summary>
    ///  手描き入力開始の記録.
    /// </summary>
    class BeginStrokeTrace : StrokeTraceWithPositionBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public BeginStrokeTrace()
            : base(InputTraceKind.BeginStroke)
        {
        }
    }

    /// <summary>
    ///  手描き入力移動の記録.
    /// </summary>
    class MoveStrokeTrace : StrokeTraceWithPositionBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public MoveStrokeTrace()
            : base(InputTraceKind.MoveStroke)
        {
        }
    }

    /// <summary>
    ///  手描き入力終了の記録.
    /// </summary>
    class EndStrokeTrace : StrokeTraceWithPositionBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public EndStrokeTrace()
            : base(InputTraceKind.EndStroke)
        {
        }
    }

    /// <summary>
    ///  手描き入力削除の記録.
    /// </summary>
    class RemoveStrokeTrace : StrokeTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public RemoveStrokeTrace()
            : base(InputTraceKind.RemoveStroke)
        {
        }
    }

    /// <summary>
    ///  手描き入力の色指定の記録.
    /// </summary>
    class SetStrokeColorTrace : StrokeTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public SetStrokeColorTrace()
            : base(InputTraceKind.SetStrokeColor)
        {
        }

        /// <summary>
        ///  色.
        /// </summary>
        public Windows.UI.Color Color { get; set; }

        /// <summary>
        ///  JSONとして保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.A));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.R));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.G));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.B));
        }

        /// <summary>
        ///  JSONから復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            byte a = (byte)jsonArray.GetNumberAt(index);
            byte r = (byte)jsonArray.GetNumberAt(index+1);
            byte g = (byte)jsonArray.GetNumberAt(index+2);
            byte b = (byte)jsonArray.GetNumberAt(index+3);
            this.Color = Windows.UI.Color.FromArgb(a, r, g, b);
            return index + 4;
        }
    }

    /// <summary>
    ///  手描き入力の幅指定の記録.
    /// </summary>
    class SetStrokeThicknessTrace : StrokeTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public SetStrokeThicknessTrace()
            : base(InputTraceKind.SetStrokeThickness)
        {
        }

        /// <summary>
        ///  幅.
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        ///  JSONとして保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateNumberValue(this.Thickness));
        }

        /// <summary>
        ///  JSONから復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            this.Thickness = jsonArray.GetNumberAt(index);
            return index + 1;
        }
    }

    /// <summary>
    ///  背景の色指定の記録.
    /// </summary>
    class SetBackgroundColorTrace : InputTraceBase
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public SetBackgroundColorTrace()
            : base(InputTraceKind.SetBackgroundColor)
        {
        }

        /// <summary>
        ///  色.
        /// </summary>
        public Windows.UI.Color Color { get; set; }

        /// <summary>
        ///  JSONとして保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.A));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.R));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.G));
            jsonArray.Add(JsonValue.CreateNumberValue((double)this.Color.B));
        }

        /// <summary>
        ///  JSONから復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            byte a = (byte)jsonArray.GetNumberAt(index);
            byte r = (byte)jsonArray.GetNumberAt(index + 1);
            byte g = (byte)jsonArray.GetNumberAt(index + 2);
            byte b = (byte)jsonArray.GetNumberAt(index + 3);
            this.Color = Windows.UI.Color.FromArgb(a, r, g, b);
            return index + 4;
        }
    }

    /// <summary>
    ///  ファイルの生成元.
    /// </summary>
    enum InputTraceFileSource
    {
        /// <summary>
        ///  ファイル無し.
        /// </summary>
        None, 

        /// <summary>
        ///  記録済みのTraceをロードした.
        /// </summary>
        LoadFromTrace, 

        /// <summary>
        ///  このアプリ内で新規作成.
        /// </summary>
        CreateNew, 

        /// <summary>
        ///  OpenPickerで開いた.
        /// </summary>
        OpenPicker, 
    }

    /// <summary>
    ///  ファイルを扱う必要のあるTrace.
    /// </summary>
    interface IInputTraceWithFile
    {
        InputTraceFileSource Source { get; set; }
        string FileName { get; set; }
        Windows.Storage.StorageFile File { get; set; }
    }

    /// <summary>
    ///  画像指定の記録.
    /// </summary>
    class SetImageTrace : InputTraceBase, IInputTraceWithFile
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public SetImageTrace()
            : base(InputTraceKind.SetImage)
        {
            this.Source = InputTraceFileSource.None;
            this.FileName = String.Empty;
        }

        /// <summary>
        ///  ファイルの生成元.
        /// </summary>
        public InputTraceFileSource Source { get; set; }

        /// <summary>
        ///  画像のファイル名.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        ///  画像のファイル.
        /// </summary>
        public Windows.Storage.StorageFile File { get; set; }

        /// <summary>
        ///  JSONとして保存する.
        /// </summary>
        /// <returns></returns>
        public override void SaveAsJson(JsonArray jsonArray)
        {
            base.SaveAsJson(jsonArray);
            jsonArray.Add(JsonValue.CreateStringValue(this.FileName));
        }

        /// <summary>
        ///  JSONから復元する.
        /// </summary>
        /// <param name="jsonobj"></param>
        public override uint LoadFromJson(JsonArray jsonArray)
        {
            uint index = base.LoadFromJson(jsonArray);
            this.FileName = jsonArray.GetStringAt(index);
            return index + 1;
        }

    }
}
