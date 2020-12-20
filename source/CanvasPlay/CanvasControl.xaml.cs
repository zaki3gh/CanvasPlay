using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;


// ユーザー コントロールのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace MyApps.CanvasPlay
{
    public sealed partial class CanvasControl : UserControl
    {
        /// <summary>
        ///  Static Constructor.
        /// </summary>
        static CanvasControl()
        {
            InitProperties();
        }

        /// <summary>
        ///  Constructor.
        /// </summary>
        public CanvasControl()
        {
            this.InitializeComponent();

            this.DataContext = this;
        }

        /// <summary>
        ///  描画された入力をすべて削除する.
        /// </summary>
        public void ClearCanvas()
        {
            this.MainCanvas.Children.Clear();
            this.canvasElementsDictionay.Clear();
            //this.BackgroundColor = Windows.UI.Colors.Transparent;
            this.layoutRoot.Background = null;
            this.canvasImage.Source = null;
        }

        /// <summary>
        ///  記録された入力を再生する.
        /// </summary>
        public async void PlayAsync(long firstTimestamp)
        {
            this.IsPlaying = true;
            using (this.cancellationTokenSource = new System.Threading.CancellationTokenSource())
            {
                try
                {
                    await PlayCoreAsync(firstTimestamp);
                }
                // キャンセルされた
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken != this.cancellationTokenSource.Token)
                    {
                        throw;
                    }
                }
            }
            this.cancellationTokenSource = null;
            this.IsPlaying = false;
        }

        /// <summary>
        ///  再生を停止する.
        /// </summary>
        public void StopPlaying()
        {
            if (this.cancellationTokenSource == null)
            {
                return;
            }

            this.cancellationTokenSource.Cancel();
        }

        /// <summary>
        ///  再生を一時停止する.
        /// </summary>
        public void PausePlaying()
        {
            StopPlaying();
            this.firstTimestampResume = this.CurrentPlayTime.Ticks;
        }

        /// <summary>
        ///  停止した場所から再生を開始する.
        /// </summary>
        public void ResumePlay()
        {
            MergeNewTraces();
            PlayAsync(this.firstTimestampResume);
        }

        private long firstTimestampResume;

        /// <summary>
        ///  記録された入力を再生する.
        /// </summary>
        public async Task PlayCoreAsync(long firstTimestamp)
        {
            if (!this.Item.InputRecorder.HasTrace)
            {
                return;
            }

            this.CurrentPlayTime = TimeSpan.FromTicks(firstTimestamp);
            var playTraceStartTick = DateTimeOffset.UtcNow.Ticks - firstTimestamp;
            var firstTraceTime = this.Item.InputRecorder.Traces[0].Timestamp;
            var currentTraceTime = firstTimestamp;

            this.playStartTicks = playTraceStartTick;

            foreach (var trace in this.Item.InputRecorder.Traces.SkipWhile(x=>x.Timestamp<firstTimestamp))
            {
                if (this.cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                // 時間調整
                if (currentTraceTime < trace.Timestamp)
                {
                    var timeFromStart = DateTimeOffset.UtcNow.Ticks - playTraceStartTick;
                    var nextTraceTime = trace.Timestamp - firstTraceTime;
                    if (timeFromStart < nextTraceTime)
                    {
                        await Task.Delay((int)(nextTraceTime - timeFromStart) / 10000, this.cancellationTokenSource.Token);
                    }
                }

                // 再生の実行
                switch (trace.Kind)
                {
                    case InputTraceKind.BeginStroke:
                        BeginStroke(trace as BeginStrokeTrace);
                        break;
                    case InputTraceKind.MoveStroke:
                        MoveStroke(trace as MoveStrokeTrace);
                        break;
                    case InputTraceKind.EndStroke:
                        EndStroke(trace as EndStrokeTrace);
                        break;
                    case InputTraceKind.RemoveStroke:
                        RemoveStroke(trace as RemoveStrokeTrace);
                        break;
                    case InputTraceKind.SetStrokeColor:
                        SetStrokeColor(trace as SetStrokeColorTrace);
                        break;
                    case InputTraceKind.SetStrokeThickness:
                        SetStrokeThickness(trace as SetStrokeThicknessTrace);
                        break;
                    case InputTraceKind.SetBackgroundColor:
                        SetBackgroundColor(trace as SetBackgroundColorTrace);
                        break;
                    case InputTraceKind.SetImage:
                        await SetImageAsync(trace as SetImageTrace);
                        break;
                }

                // 時間記録の更新
                currentTraceTime = trace.Timestamp;
                this.CurrentPlayTime = TimeSpan.FromTicks(currentTraceTime - firstTraceTime);
            }
        }

        /// <summary>
        ///  新しく編集された入力を記録に追加する.
        /// </summary>
        public void MergeNewTraces()
        {
            if (this.inputRecorderForNewTrace.HasDeferredTraces)
            {
                this.inputRecorderForNewTrace.MergeDeferredTraces();
            }
            if (this.inputRecorderForNewTrace.HasTrace)
            {
                this.Item.InputRecorder.Merge(this.inputRecorderForNewTrace, this.playStartTicks);
            }
            CancelNewTraces();
        }

        /// <summary>
        ///  新しく編集された入力を取り消す.
        /// </summary>
        public void CancelNewTraces()
        {
            this.inputRecorderForNewTrace.Clear();
        }

        #region Properties

        /// <summary>
        ///  プロパティを初期化する.
        /// </summary>
        private static void InitProperties()
        {
            IsEditingProperty = DependencyProperty.Register("IsEditing", typeof(bool), typeof(CanvasControl), 
                new PropertyMetadata(false, OnIsEditingPropertyChanged));
            IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(CanvasControl),
                null);
            CurrentPlayTimeProperty = DependencyProperty.Register("CurrentPlayTime", typeof(TimeSpan), typeof(CanvasControl),
                null);

            StrokeColorProperty = DependencyProperty.Register("StrokeColor", typeof(Windows.UI.Color), typeof(CanvasControl),
                new PropertyMetadata(Windows.UI.Colors.Blue));
            StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CanvasControl),
                new PropertyMetadata(10.0));

            BackgroundColorProperty = DependencyProperty.Register("BackgroundColor", typeof(Windows.UI.Color), typeof(CanvasControl),
                new PropertyMetadata(Windows.UI.Colors.Transparent, OnBackgroundColorPropertyChanged));
        }

        /// <summary>
        ///  手描き入力.
        /// </summary>
        public CanvasItem Item { get; set; }

        /// <summary>
        ///  新規に追加された手描き入力があるかどうか.
        /// </summary>
        public bool HasNewTraces
        {
            get
            {
                return this.inputRecorderForNewTrace.HasTrace;
            }
        }

        /// <summary>
        ///  IsEditingプロパティ.
        /// </summary>
        public static DependencyProperty IsEditingProperty { get; private set; }

        /// <summary>
        ///  編集中かどうか.
        /// </summary>
        public bool IsEditing
        {
            get { return (bool)GetValue(IsEditingProperty); }
            set { SetValue(IsEditingProperty, value); }
        }

        /// <summary>
        ///  IsEditingプロパティにこのクラス内からアクセスするための補助変数.
        /// </summary>
        private bool isEditingInternal = false;

        /// <summary>
        ///  IsEditingPropertyの変更時の処理(static).
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsEditingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((CanvasControl)d).OnIsEditingPropertyChanged(e);
        }

        /// <summary>
        ///  IsEditingPropertyの変更時の処理.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void OnIsEditingPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            this.isEditingInternal = (bool)e.NewValue;
        }

        /// <summary>
        ///  IsPlayingプロパティ.
        /// </summary>
        public static DependencyProperty IsPlayingProperty { get; private set; }

        /// <summary>
        ///  再生中かどうか.
        /// </summary>
        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            private set { SetValue(IsPlayingProperty, value); }
        }

        /// <summary>
        ///  CurrentPlayTimeプロパティ.
        /// </summary>
        public static DependencyProperty CurrentPlayTimeProperty { get; private set; }

        /// <summary>
        ///  再生中の入力の経過時間.
        /// </summary>
        public TimeSpan CurrentPlayTime
        {
            get { return (TimeSpan)GetValue(CurrentPlayTimeProperty); }
            set { SetValue(CurrentPlayTimeProperty, value); }
        }

        /// <summary>
        ///  StrokeColorプロパティ.
        /// </summary>
        public static DependencyProperty StrokeColorProperty { get; private set; }

        /// <summary>
        ///  手描き入力の色.
        /// </summary>
        public Windows.UI.Color StrokeColor
        {
            get { return (Windows.UI.Color)GetValue(StrokeColorProperty); }
            set { SetValue(StrokeColorProperty, value); }
        }

        /// <summary>
        ///  StrokeThicknessプロパティ.
        /// </summary>
        public static DependencyProperty StrokeThicknessProperty { get; private set; }

        /// <summary>
        ///  手描き入力の太さ.
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        ///  BackgroundColorプロパティ.
        /// </summary>
        public static DependencyProperty BackgroundColorProperty { get; private set; }

        /// <summary>
        ///  背景の色.
        /// </summary>
        public Windows.UI.Color BackgroundColor
        {
            get { return (Windows.UI.Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        ///  背景が設定されているかどうか.
        /// </summary>
        public bool IsBackgroundSet { get { return this.layoutRoot.Background != null; } }

        /// <summary>
        ///  BackgroundColorPropertyの変更時の処理(static).
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnBackgroundColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue.Equals(e.OldValue))
            {
                return;
            }
            (d as CanvasControl).OnBackgroundColorPropertyChanged(e);
        }

        /// <summary>
        ///  BackgroundColorPropertyの変更時の処理.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private void OnBackgroundColorPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            SaveSetBackgroundColorTrace();
        }

        #endregion

        private Dictionary<uint, Windows.UI.Xaml.Shapes.Polyline> pressedPointerDictionay = new Dictionary<uint, Windows.UI.Xaml.Shapes.Polyline>();
        private Dictionary<uint, BeginStrokeTrace> strokeCandidateDictionary = new Dictionary<uint, BeginStrokeTrace>();

        private Dictionary<uint, Windows.UI.Xaml.UIElement> canvasElementsDictionay = new Dictionary<uint, Windows.UI.Xaml.UIElement>();

        private InputTraceRecorder inputRecorderForNewTrace = new InputTraceRecorder();
        private long playStartTicks = 0;

        /// <summary>
        ///  再生を停止するための<c>CancellationTokenSource</c>.
        /// </summary>
        private System.Threading.CancellationTokenSource cancellationTokenSource;


        /// <summary>
        ///  手描きの入力を開始するときに呼び出す.
        /// </summary>
        private Windows.UI.Xaml.Shapes.Polyline BeginStroke(BeginStrokeTrace trace)
        {
            var line = new Windows.UI.Xaml.Shapes.Polyline()
            {
                //Stroke = new SolidColorBrush(Windows.UI.Colors.Blue),
                //StrokeThickness = 10,
                Tag = trace.Index,
            };

            line.Points.Add(trace.Position);
            this.MainCanvas.Children.Add(line);

//            System.Diagnostics.Debug.WriteLine("BeginStroke: {0}, {1}", trace.Index, trace.Position);
            this.canvasElementsDictionay.Add(trace.Index, line);

            return line;
        }

        /// <summary>
        ///  手描きの入力が移動しているときに呼び出す.
        /// </summary>
        /// <param name="pointer"></param>
        private void MoveStroke(MoveStrokeTrace trace)
        {
            var elem = GetCanvasElement<Windows.UI.Xaml.Shapes.Polyline>(trace.Index);
            if (elem == null)
            {
                return;
            }

            elem.Points.Add(trace.Position);
        }

        /// <summary>
        ///  手描きの入力が終わったときに呼び出す.
        /// </summary>
        /// <param name="pointer"></param>
        private void EndStroke(EndStrokeTrace trace)
        {
            var elem = GetCanvasElement<Windows.UI.Xaml.Shapes.Polyline>(trace.Index);
            if (elem == null)
            {
                return;
            }

            elem.Points.Add(trace.Position);
        }

        /// <summary>
        ///  手描きの入力を削除する.
        /// </summary>
        /// <param name="index"></param>
        private void RemoveStroke(RemoveStrokeTrace trace)
        {
            var elem = GetCanvasElement<UIElement>(trace.Index);
            if (elem == null)
            {
                return;
            }

            this.MainCanvas.Children.Remove(elem);
        }

        /// <summary>
        ///  手描き入力の色を設定する.
        /// </summary>
        /// <param name="trace"></param>
        private void SetStrokeColor(SetStrokeColorTrace trace)
        {
            var elem = GetCanvasElement<Windows.UI.Xaml.Shapes.Polyline>(trace.Index);
            if (elem == null)
            {
                return;
            }

            elem.Stroke = new SolidColorBrush(trace.Color);
        }

        /// <summary>
        ///  手描き入力の太さを設定する.
        /// </summary>
        /// <param name="trace"></param>
        private void SetStrokeThickness(SetStrokeThicknessTrace trace)
        {
            var elem = GetCanvasElement<Windows.UI.Xaml.Shapes.Polyline>(trace.Index);
            if (elem == null)
            {
                return;
            }

            elem.StrokeThickness = trace.Thickness;
        }

        /// <summary>
        ///  背景の色を設定する.
        /// </summary>
        public void SaveSetBackgroundColorTrace()
        {
            var bgColor = this.BackgroundColor;
            var colorbrush = this.layoutRoot.Background as SolidColorBrush;
            System.Diagnostics.Debug.WriteLine("SaveSetBackgroundColorTrace: {0}", bgColor);

            // 今透明
            //   --> 透明にするなら記録の必要なし
            // 今透明でない
            //   --> 同じ色にするなら記録の必要なし
            if ((colorbrush == null) || (colorbrush.Color.A == 0))
            {
                if (bgColor.A == 0)
                {
                    return;
                }
            }
            else
            {
                if (bgColor == colorbrush.Color)
                {
                    return;
                }
            }

            var trace = new SetBackgroundColorTrace()
            {
                Timestamp = DateTimeOffset.UtcNow.Ticks,
                Color = this.BackgroundColor,
            };
            this.inputRecorderForNewTrace.Add(trace);
            SetBackgroundColor(trace);
        }

        /// <summary>
        ///  背景の色を設定する.
        /// </summary>
        /// <param name="trace"></param>
        private void SetBackgroundColor(SetBackgroundColorTrace trace)
        {
            System.Diagnostics.Debug.WriteLine("SetBackgroundColorTrace: {0}", trace.Color);
            //var colorbrush = this.layoutRoot.Background as SolidColorBrush;
            //if ((colorbrush != null) && (colorbrush.Color == trace.Color))
            //{
            //    return;
            //}
            //else if ((colorbrush == null) && (this.BackgroundColor.A == 0))
            //{
            //    return;
            //}

            this.layoutRoot.Background = new SolidColorBrush(trace.Color);
        }

        /// <summary>
        ///  手描き入力された描画部品を取得する.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="index">識別番号</param>
        /// <returns></returns>
        private T GetCanvasElement<T>(uint index) where T : class
        {
            UIElement element;
            if (!this.canvasElementsDictionay.TryGetValue(index, out element))
            {
                return null;
            }

            return element as T;
        }

        #region Event Handlers

        /// <summary>
        ///  手描き描画用CanvasのPointerPressedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // 編集中以外は無視する
            if (!this.isEditingInternal)
            {
                return;
            }

            var pointerPoint = e.GetCurrentPoint(this.MainCanvas);

            var trace = new BeginStrokeTrace()
            {
                // Indexは実際に描画するときに設定する
                // Index = this.inputRecorder.GetNextStrokeIndex(), 
                Position = pointerPoint.Position,
                Timestamp = DateTimeOffset.UtcNow.Ticks,
            };

            // 全然移動しなくて手描き入力しない方がよいかもしれないので候補リストに登録するだけにしておく
            strokeCandidateDictionary[pointerPoint.PointerId] = trace;
        }

        /// <summary>
        ///  手描き描画用CanvasのPointerMovedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // 編集中以外は無視する
            if (!this.isEditingInternal)
            {
                return;
            }

            Windows.UI.Xaml.Shapes.Polyline line;
            if (!this.pressedPointerDictionay.TryGetValue(e.Pointer.PointerId, out line))
            {
                BeginStrokeTrace beginTrace;
                if (!this.strokeCandidateDictionary.TryGetValue(e.Pointer.PointerId, out beginTrace))
                {
                    return;
                }
                if (!CallBeginTraceIfMovingEnough(e))
                {
                    return;
                }
                if (!this.pressedPointerDictionay.TryGetValue(e.Pointer.PointerId, out line))
                {
                    return;
                }
            }

            var pointerPoint = e.GetCurrentPoint(this.MainCanvas);
            var trace = new MoveStrokeTrace()
            {
                Index = (uint)line.Tag,
                Position = pointerPoint.Position,
                Timestamp = DateTimeOffset.UtcNow.Ticks,
            };
            // this.Item.InputRecorder.Add(trace);
            this.inputRecorderForNewTrace.Add(trace);

            MoveStroke(trace);
        }

        /// <summary>
        ///  手描き描画用CanvasのPointerReleasedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            // 編集中以外は無視する
            if (!this.isEditingInternal)
            {
                return;
            }

            ProcessEndStroke(e);
        }

        /// <summary>
        ///  手描き描画用CanvasのPointerExitedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // 編集中以外は無視する
            if (!this.isEditingInternal)
            {
                return;
            }

            ProcessEndStroke(e);
        }

        /// <summary>
        ///  手描き描画用CanvasのRightTappedイベントを処理する.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainCanvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // 編集中以外は無視する
            if (!this.isEditingInternal)
            {
                return;
            }

            var stroke = e.OriginalSource as Windows.UI.Xaml.Shapes.Polyline;
            if ((stroke == null) || (stroke.Tag == null))
            {
                return;
            }

            e.Handled = true;
            var trace = new RemoveStrokeTrace()
            {
                Index = (uint)stroke.Tag,
                Timestamp = DateTimeOffset.UtcNow.Ticks,
            };
            // this.Item.InputRecorder.Add(trace);
            this.inputRecorderForNewTrace.Add(trace);

            RemoveStroke(trace);
        }

        /// <summary>
        ///  PointerPressedされた場所から十分に移動したら手描き入力開始として処理する.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool CallBeginTraceIfMovingEnough(PointerRoutedEventArgs e)
        {
            BeginStrokeTrace trace;
            if (!this.strokeCandidateDictionary.TryGetValue(e.Pointer.PointerId, out trace))
            {
                return false;
            }

            var distance = this.StrokeThickness;
            var pointerPoint = e.GetCurrentPoint(this.MainCanvas);
            if ((Math.Abs(trace.Position.X - pointerPoint.Position.X) < distance) &&
                (Math.Abs(trace.Position.Y - pointerPoint.Position.Y) < distance))
            {
                return false;
            }

            this.strokeCandidateDictionary.Remove(e.Pointer.PointerId);

            var timestamp = trace.Timestamp;

            trace.Index = this.Item.InputRecorder.GetNextStrokeIndex();

            // this.Item.InputRecorder.Add(trace);
            this.inputRecorderForNewTrace.Add(trace);
            var elem = BeginStroke(trace);
            this.pressedPointerDictionay.Add(e.Pointer.PointerId, elem);

            // properties
            // - color
            // - thickness
            var colortrace = new SetStrokeColorTrace()
            {
                Timestamp = timestamp,
                Index = trace.Index,
                Color = this.StrokeColor,
            };
            SetStrokeColor(colortrace);
            // this.Item.InputRecorder.Add(colortrace);
            this.inputRecorderForNewTrace.Add(colortrace);

            var thicknesstrace = new SetStrokeThicknessTrace()
            {
                Timestamp = timestamp,
                Index = trace.Index,
                Thickness = this.StrokeThickness,
            };
            SetStrokeThickness(thicknesstrace);
            // this.Item.InputRecorder.Add(thicknesstrace);
            this.inputRecorderForNewTrace.Add(thicknesstrace);

            return true;
        }

        /// <summary>
        ///  手描き入力終了を処理する.
        /// </summary>
        /// <param name="e"></param>
        private void ProcessEndStroke(PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(this.MainCanvas);
            Windows.UI.Xaml.Shapes.Polyline line;
            if (!this.pressedPointerDictionay.TryGetValue(pointerPoint.PointerId, out line))
            {
                if (this.strokeCandidateDictionary.ContainsKey(pointerPoint.PointerId))
                {
                    this.strokeCandidateDictionary.Remove(pointerPoint.PointerId);
                }
                return;
            }

            var trace = new EndStrokeTrace()
            {
                Index = (uint)line.Tag,
                Position = pointerPoint.Position,
                Timestamp = DateTimeOffset.UtcNow.Ticks,
            };
            // this.Item.InputRecorder.Add(trace);
            this.inputRecorderForNewTrace.Add(trace);

            EndStroke(trace);
            this.pressedPointerDictionay.Remove(pointerPoint.PointerId);
        }

        #endregion

        public async Task SetImageAsync(Windows.Storage.StorageFile imageFile, bool deferred=false)
        {
            var trace = new SetImageTrace()
            {
                Timestamp = DateTime.UtcNow.Ticks, 
                FileName = imageFile.Name, 
                File = imageFile,
                Source = InputTraceFileSource.CreateNew,
            };

            await SetImageAsync(trace);

            if (deferred)
            {
                this.inputRecorderForNewTrace.AddDeferredTrace(trace);
            }
            else
            {
                this.inputRecorderForNewTrace.Add(trace);
            }
        }

        private async Task SetImageAsync(SetImageTrace trace)
        {
            using (var strm = await trace.File.OpenReadAsync())
            {
                await SetImageAsync(strm);
            }
        }

        public async Task SetImageAsync(Windows.Storage.Streams.IRandomAccessStream imageStream)
        {
            var bmp = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            await bmp.SetSourceAsync(imageStream);
            this.canvasImage.Source = bmp;
        }
    }
}
