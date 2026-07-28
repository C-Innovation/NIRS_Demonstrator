using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class Series : Polyline, IDisposable
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private Canvas _ChartArea;
        private HorizontalAxi _AxisX;
        private VerticalAxi _AxisY;
        private Pair<double, double> _PointsViewHorizontalBorders;
        private Pair<double, double> _PointsViewVerticalBorders;
        private PointDeque _PointsTotal;
        private Pair<double, double> _PointsTotalBorders;
        private ChartMode _ChartMode;

        /// <summary>
        /// Два буфера экранных точек, используемых по очереди. Присваивание
        /// <see cref="Polyline.Points"/> нужно, чтобы Avalonia перестроила геометрию,
        /// а чередование даёт новую ссылку без выделения списка на каждый кадр.
        /// </summary>
        private List<Point> _RenderBufferA;
        private List<Point> _RenderBufferB;
        private bool _UseRenderBufferA;

        private bool _IsDisposed;

        #endregion

        #region Public Properties
        public bool IsValid { get; set; } = false;
        public ObservableCollection<VerticalMarker> VerticalMarkers { get; set; }

        public VerticalSelection SelectionArea { get; set; }
        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor
        public Series()
        {
            IsValid = false;

        }
        /// <summary>
        /// Extended constructor
        /// </summary>
        public Series(Canvas chartArea, HorizontalAxi axisX, VerticalAxi axisY, ChartMode chartMode = ChartMode.Live)
        {
            SetParams(chartArea, axisX, axisY, chartMode);
        }





        #endregion

        #region Private Callbacks

        private async void _ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            //_MutexUpdateChart.WaitOne();
            await UpdatePointsViewAsync();
            await UpdateMarkersAsync();
            //_MutexUpdateChart.ReleaseMutex();
        }

        private async void _AxisX_AxisMinValueChanged(object? sender, double e)
        {
#if DEBUG
            //Debugger.Break();
#endif
            _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
            _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;

            await UpdatePointsViewAsync();
            await UpdateMarkersAsync();
            //_PointsViewHorizontalBorders.First = _AxisX.AxisMinValue;
            //_PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
        }

        private async void _AxisX_AxisSizeChanged(object? sender, double e)
        {
            if (_ChartMode == ChartMode.Live)
            {
                _PointsTotal.Clear();
            }
            else if (_ChartMode == ChartMode.Static)
            {
                _PointsTotal.Clear();
                /// TODO: Add handle
            }

            //_MutexUpdateChart.WaitOne();
            await UpdatePointsViewAsync();
            await UpdateMarkersAsync();
            //_MutexUpdateChart.ReleaseMutex();
        }


        private async void _AxisY_AxisMinValueChanged(object? sender, double e)
        {
            _PointsViewVerticalBorders.First = _AxisY.AxisMinValue;
            _PointsViewVerticalBorders.Second = _AxisY.AxisMaxValue;
            await UpdatePointsViewAsync();
        }

        private async void _AxisY_AxisSizeChanged(object? sender, double e)
        {
            //_MutexUpdateChart.WaitOne();
            await UpdatePointsViewAsync();
            //_MutexUpdateChart.ReleaseMutex();
        }

        private void VerticalMarkers_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is VerticalMarker marker)
                            _ChartArea.Children.Add(marker);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is VerticalMarker marker)
                            _ChartArea.Children.Remove(marker);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        if (item is VerticalMarker marker)
                            _ChartArea.Children.Remove(marker);
                    }
                    foreach (var item in e.NewItems)
                    {
                        if (item is VerticalMarker marker)
                            _ChartArea.Children.Add(marker);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:

                    VerticalMarkers.Clear();
                    VerticalMarkers = new ObservableCollection<VerticalMarker>();
                    break;

                default: break;
            }

            foreach (VerticalMarker marker in VerticalMarkers)
            {
                if (!marker.IsValid)
                {
                    marker.SetParams(_ChartArea, _AxisX);
                    marker.Stroke = this.Stroke;
                    marker.Opacity = 0.75;
                    marker.PointsView = _PointsViewHorizontalBorders;
                    marker.UpdateMarker();
                }
            }
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        public void SetParams(Canvas chartArea, HorizontalAxi axisX, VerticalAxi axisY, ChartMode chartMode = ChartMode.Live)
        {
            // Повторная привязка к другой области построения не должна оставлять
            // подписки на предыдущей.
            DetachHandlers();

            VerticalMarkers = new ObservableCollection<VerticalMarker>();
            VerticalMarkers.CollectionChanged += VerticalMarkers_CollectionChanged;

            _ChartMode = chartMode;

            _ChartArea = chartArea;
            _ChartArea.SizeChanged += _ChartArea_SizeChanged;

            _AxisX = axisX;
            _AxisX.AxisSizeChanged += _AxisX_AxisSizeChanged;
            _AxisX.AxisMinValueChanged += _AxisX_AxisMinValueChanged;

            _AxisY = axisY;
            _AxisY.AxisSizeChanged += _AxisY_AxisSizeChanged;
            _AxisY.AxisMinValueChanged += _AxisY_AxisMinValueChanged;
            _PointsViewVerticalBorders.First = _AxisY.AxisMinValue;
            _PointsViewVerticalBorders.Second = _AxisY.AxisMaxValue;
            StrokeThickness = 2;

            _PointsTotal = new PointDeque();
            _RenderBufferA = new List<Point>();
            _RenderBufferB = new List<Point>();
            _UseRenderBufferA = true;

            SelectionArea = new VerticalSelection(_ChartArea, _AxisX);
            SelectionArea.Fill = this.Stroke;
            _ChartArea.Children.Add(SelectionArea);
            IsValid = true;
        }

        /// <summary>
        /// Отписывается от области построения и осей. Без этого удалённая серия
        /// остаётся достижимой из долгоживущих Canvas/Axi вместе со всеми накопленными
        /// точками — классическая утечка через обработчики событий.
        /// </summary>
        public void DetachHandlers()
        {
            if (VerticalMarkers != null)
                VerticalMarkers.CollectionChanged -= VerticalMarkers_CollectionChanged;

            if (_ChartArea != null)
            {
                _ChartArea.SizeChanged -= _ChartArea_SizeChanged;

                if (SelectionArea != null)
                    _ChartArea.Children.Remove(SelectionArea);

                if (VerticalMarkers != null)
                {
                    foreach (VerticalMarker marker in VerticalMarkers)
                        _ChartArea.Children.Remove(marker);
                }
            }

            if (_AxisX != null)
            {
                _AxisX.AxisSizeChanged -= _AxisX_AxisSizeChanged;
                _AxisX.AxisMinValueChanged -= _AxisX_AxisMinValueChanged;
            }

            if (_AxisY != null)
            {
                _AxisY.AxisSizeChanged -= _AxisY_AxisSizeChanged;
                _AxisY.AxisMinValueChanged -= _AxisY_AxisMinValueChanged;
            }

            IsValid = false;
        }

        public void Dispose()
        {
            if (_IsDisposed)
                return;

            _IsDisposed = true;
            DetachHandlers();
            _PointsTotal?.Clear();
            _RenderBufferA?.Clear();
            _RenderBufferB?.Clear();
        }



        public Task AddPointAsync(Point point)
        {
            return AddPointsRangeAsync(new[] { point });
        }

        /// <summary>
        /// Добавление точек и перерисовка выполняются одной операцией в потоке UI:
        /// иначе фоновый поток мог бы менять буфер точек ровно в тот момент,
        /// когда поток UI по нему проходит.
        /// </summary>
        public async Task AddPointsRangeAsync(IEnumerable<Point> points)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                AddPointsRangeCore(points);
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() => AddPointsRangeCore(points));
        }

        private void AddPointsRangeCore(IEnumerable<Point> points)
        {
            if (_IsDisposed)
                return;

            foreach (Point point in points)
            {
                PrepareSeriesPoints(point);
            }

            UpdatePointsViewCore();
        }

        public async Task ClearPoints()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ClearPointsCore();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(ClearPointsCore);
        }

        private void ClearPointsCore()
        {
            _PointsTotal.Clear();
            _RenderBufferA.Clear();
            _RenderBufferB.Clear();
            this.Points = _UseRenderBufferA ? _RenderBufferA : _RenderBufferB;
        }

        public void AddMarker(double Level)
        {
            VerticalMarker marker = new VerticalMarker()
            {
                Level = Level,
                PointsView = _PointsViewHorizontalBorders,

            };
            VerticalMarkers.Add(marker);
        }

        #endregion

        #region Private Methods

        private async Task UpdatePointsViewAsync()
        {
            // Раньше здесь был Task.Run поверх блокирующего Dispatcher.Invoke:
            // поток из пула занимался только тем, что ждал поток UI.
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdatePointsViewCore();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(UpdatePointsViewCore);
        }

        /// <summary>
        /// Пересчитывает видимые точки в экранные координаты. Только поток UI.
        /// </summary>
        private void UpdatePointsViewCore()
        {
            if (_IsDisposed || _PointsTotal == null)
                return;

            List<Point> target = _UseRenderBufferA ? _RenderBufferA : _RenderBufferB;
            _UseRenderBufferA = !_UseRenderBufferA;
            target.Clear();

            if (_PointsTotal.Count != 0)
            {
                // Точки упорядочены по X, поэтому границы окна ищутся бинарным поиском,
                // а не полным проходом по всей истории на каждую перерисовку.
                int from = _PointsTotal.LowerBound(_PointsViewHorizontalBorders.First);
                int to = _PointsTotal.UpperBound(_PointsViewHorizontalBorders.Second);

                double areaWidth = _ChartArea.Bounds.Width;
                double areaHeight = _ChartArea.Bounds.Height;
                double axisSizeX = _AxisX.AxisSize;
                double axisSizeY = _AxisY.AxisSize;
                double left = _PointsViewHorizontalBorders.First;
                double bottom = _PointsViewVerticalBorders.First;
                double top = _PointsViewVerticalBorders.Second;

                if (target.Capacity < to - from)
                    target.Capacity = to - from;

                for (int i = from; i < to; i++)
                {
                    Point point = _PointsTotal[i];

                    double x = ((point.X - left) / axisSizeX) * areaWidth;
                    double y;

                    if (point.Y <= bottom)
                        y = areaHeight;
                    else if (point.Y >= top)
                        y = 0;
                    else
                        y = areaHeight - (((point.Y - bottom) / axisSizeY) * areaHeight);

                    target.Add(new Point(x, y));
                }
            }

            this.Points = target;
        }

        private async Task UpdateMarkersAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {

                for (int i = 0; i < VerticalMarkers.Count; i++)
                {
                    if (_ChartMode == ChartMode.Live)
                    {
                        if (VerticalMarkers[i].Level < _PointsTotalBorders.First)
                        {
                            VerticalMarkers.Remove(VerticalMarkers[i]);

                            continue;
                        }
                    }
                    else if (_ChartMode == ChartMode.Static)
                    {
                        // TODO: Add handler
                    }
                    VerticalMarkers[i].PointsView = _PointsViewHorizontalBorders;
                    VerticalMarkers[i].UpdateMarker();
                }
                if(SelectionArea.Fill == null)
                    SelectionArea.Fill = this.Stroke;
                SelectionArea.PointsView = _PointsViewHorizontalBorders;
                SelectionArea.Update();
            });

        }

        private void PrepareSeriesPoints(Point point)
        {
            if (_PointsTotal.Count == 0)
            {
                //if (_ChartMode == ChartMode.Live)
                //{
                    _PointsTotalBorders.First = point.X;
                    _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);

                    _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                    _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                //} //await UpdateMarkersAsync();
                //else if (_ChartMode == ChartMode.Static)
                //{
                //    _PointsTotalBorders.First = point.X;
                //    _PointsTotalBorders.Second = 1;
                //}
            }
            else
            {
                if (_ChartMode == ChartMode.Live)
                {

                    if (point.X > _PointsTotalBorders.Second && _PointsTotal.Count > 1)
                    {
                        // O(1) вместо List.RemoveAt(0), сдвигавшего весь массив.
                        _PointsTotal.RemoveFirst();
                        _PointsTotalBorders.First = _PointsTotal.First.X;
                        _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);
                        //_AxisX.SetAxisMinValue(_AxisX.AxisMinValue + delta);
                        _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                        _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                        //await UpdateMarkersAsync();
                    }

                }
                else if (_ChartMode == ChartMode.Static)
                {
                    if (point.X > _PointsTotalBorders.Second && _PointsTotal.Count > 1)
                    {
                        double delta = _PointsTotal[1].X - _PointsTotal[0].X;
                        _PointsTotalBorders.Second += delta;
                        //_AxisX.SetAxisMinValue(_AxisX.AxisMinValue + delta);
                        _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                        _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                    }
                }


            }
            _PointsTotal.AddLast(point);
        }

        #endregion
    }

    public struct Pair<T, W>
    {
        public T First;
        public W Second;

        public Pair(T first, W second)
        {
            First = first;
            Second = second;

        }

    }

    public enum ChartMode
    {
        Static = 0,
        Live = 1,
    }
}
