using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Windows.Data.Json;


namespace MyApps.CanvasPlay
{
    /// <summary>
    ///  入力を記録する.
    /// </summary>
    public class InputTraceRecorder :
        System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        ///  Constructor.
        /// </summary>
        public InputTraceRecorder()
        {
            this.traces = new List<InputTraceBase>();
        }

        /// <summary>
        ///  記録されている入力.
        /// </summary>
        private List<InputTraceBase> traces;

        /// <summary>
        ///  記録されている手描き入力の図形の個数.
        /// </summary>
        private uint strokeIndexCount;

        /// <summary>
        ///  入力が一つ以上記録されているかどうか.
        /// </summary>
        public bool HasTrace
        {
            get
            {
                return this.traces.Count != 0;
            }
        }

        /// <summary>
        ///  記録されている入力.
        /// </summary>
        internal IReadOnlyList<InputTraceBase> Traces
        {
            get
            {
                return this.traces;
            }
        }

        public bool IsRecording { get; private set; }
        public bool IsPausing { get { return this.timePaused < 0; } }
        private long timeAsZero;
        private long timePaused = 0;

        /// <summary>
        ///  入力の継続時間.
        /// </summary>
        public TimeSpan TotalDuration
        {
            get
            {
                if (!this.HasTrace)
                {
                    return TimeSpan.Zero;
                }

                return TimeSpan.FromTicks(this.traces[this.traces.Count-1].Timestamp);
            }
        }

        //public void StartRecording(long timestamp)
        //{
        //    if (this.IsRecording)
        //    {
        //        throw new InvalidOperationException();
        //    }

        //    this.IsRecording = true;
        //    this.timeAsZero = timestamp;
        //}

        //public void Pause()
        //{
        //    this.timePaused = DateTimeOffset.UtcNow.Ticks;
        //}
        //public void Resume()
        //{
        //    var pauseDuration = DateTimeOffset.UtcNow.Ticks - this.timePaused;
        //    this.timeAsZero += pauseDuration;

        //    this.timePaused = -1;
        //}

        /// <summary>
        ///  入力を追加する.
        /// </summary>
        /// <param name="trace">追加される入力</param>
        public void Add(InputTraceBase trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }
            if (this.IsPausing)
            {
                throw new InvalidOperationException();
            }

            MergeDeferredTraces(trace);

            if (/*!this.IsRecording &&*/ !this.HasTrace)
            {
                this.IsRecording = true;
                this.timeAsZero = trace.Timestamp;
            }

            System.Diagnostics.Debug.WriteLine("{0} at {1} ({2})", trace.Kind.ToString(), trace.Timestamp, trace.Timestamp - this.timeAsZero);
            trace.Timestamp -= this.timeAsZero;
            this.traces.Add(trace);

            OnPropertyChanged("TotalDuration");
        }

        /// <summary>
        ///  次に追加される入力の直前に発生したことにする入力.
        /// </summary>
        /// <param name="trace"></param>
        public void AddDeferredTrace(InputTraceBase trace)
        {
            if (trace == null)
            {
                throw new ArgumentNullException("trace");
            }
            if (this.deferredTraces == null)
            {
                this.deferredTraces = new List<InputTraceBase>();
            }

            this.deferredTraces.Add(trace);
        }

        public void MergeDeferredTraces()
        {
            MergeDeferredTraces(null);
        }

        private void MergeDeferredTraces(InputTraceBase nextTrace)
        {
            if (this.deferredTraces == null)
            {
                return;
            }
            if (this.deferredTraces.Count == 0)
            {
                this.deferredTraces = null;
                return;
            }

            var dt = this.deferredTraces;
            this.deferredTraces = null;
            long timestamp;
            if (nextTrace != null)
            {
                timestamp = nextTrace.Timestamp - dt.Count;
            }
            else
            {
                timestamp = dt.Last().Timestamp - dt.Count;
            }
            foreach (var deferredTrace in dt)
            {
                deferredTrace.Timestamp = timestamp;
                ++timestamp;
                Add(deferredTrace);
            }
        }

        public bool HasDeferredTraces 
        {
            get { return (this.deferredTraces != null) && (this.deferredTraces.Count != 0); } 
        }

        /// <summary>
        ///  次に追加される入力の直前に発生したことにする入力.
        /// </summary>
        private List<InputTraceBase> deferredTraces;

        public void Add(IEnumerable<InputTraceBase> newTraces)
        {
            this.traces.AddRange(newTraces);
            this.traces.Sort(new InputTraceSortComparer());
            UpdateStrokeIndexForTraces();

            OnPropertyChanged("TotalDuration");
        }

        public void Merge(InputTraceRecorder recorder, long timeAsZeroOffset)
        {
            MergeDeferredTraces();

            if (timeAsZeroOffset > 0)
            {
                if (recorder.timeAsZero > timeAsZeroOffset)
                {
                    long timeOffset = recorder.timeAsZero - timeAsZeroOffset;
                    foreach (var trace in recorder.traces)
                    {
                        trace.Timestamp += timeOffset;
                    }
                }
                else
                {
                    long timeOffset = timeAsZeroOffset - recorder.timeAsZero;
                    foreach (var trace in this.traces)
                    {
                        trace.Timestamp += timeOffset;
                    }
                }
            }

            Add(recorder.traces);
        }

        public void Clear()
        {
            this.traces.Clear();
            if (this.deferredTraces != null)
            {
                this.deferredTraces.Clear();
                this.deferredTraces = null;
            }
            this.strokeIndexCount = 0;
            this.timeAsZero = 0;

            OnPropertyChanged("TotalDuration");
        }

        /// <summary>
        ///  入力を追加する.
        /// </summary>
        /// <param name="trace">追加される入力</param>
        public void SetTraces(List<InputTraceBase> traces)
        {
            if (traces == null)
            {
                throw new ArgumentNullException("traces");
            }

            this.traces = traces;
            UpdateStrokeIndexForTraces();

            OnPropertyChanged("TotalDuration");
        }

        /// <summary>
        ///  次に使う手描き入力の識別番号を取得する.
        /// </summary>
        /// <returns></returns>
        public uint GetNextStrokeIndex()
        {
            var index = this.strokeIndexCount;
            ++this.strokeIndexCount;
            return index;
        }

        private void UpdateStrokeIndexForTraces()
        {
            uint index = 0;
            foreach (var trace in this.traces)
            {
                var strace = trace as StrokeTraceBase;
                if (strace == null)
                {
                    continue;
                }
                if (strace.Index > index)
                {
                    index = strace.Index;
                }
            }

            this.strokeIndexCount = index + 1;
        }

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
    }

    /// <summary>
    ///  手描き入力の履歴を並び替えるときに使う比較.
    /// </summary>
    class InputTraceSortComparer : Comparer<InputTraceBase>
    {
        public override int Compare(InputTraceBase x, InputTraceBase y)
        {
            var timeDiff = x.Timestamp - y.Timestamp;
            if (timeDiff != 0)
            {
                return (int)timeDiff;
            }

            var kindDiff = (int)x.Kind - (int)y.Kind;
            return kindDiff;
        }
    }
}
