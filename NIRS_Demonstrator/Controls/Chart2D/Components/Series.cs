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
    public class Series : Polyline
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private Canvas _ChartArea;
        private HorizontalAxi _AxisX;
        private VerticalAxi _AxisY;
        private List<Point> _PointsView;
        private Pair<double, double> _PointsViewHorizontalBorders;
        private Pair<double, double> _PointsViewVerticalBorders;
        private List<Point> _PointsTotal;
        private Pair<double, double> _PointsTotalBorders;
        private ChartMode _ChartMode;
        private Mutex _MutexUpdateChart;

        private const int MAX_POINTS = 5000; // Ограничение точек
        private bool _isUpdating; // Флаг предотвращения рекурсии
        private readonly object _lockObj = new();
        #endregion

        #region Public Properties
        public bool IsValid { get; set; } = false;
        public ObservableCollection<VerticalMarker> VerticalMarkers { get; set; }
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
            //await UpdatePointsViewAsync();
            //await UpdateMarkersAsync();

            UpdateView();
            //_MutexUpdateChart.ReleaseMutex();
        }

        private async void _AxisX_AxisMinValueChanged(object? sender, double e)
        {
#if DEBUG
            //Debugger.Break();
#endif
            _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
            _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
            
            //await UpdatePointsViewAsync();
            //await UpdateMarkersAsync();

            UpdateView();
            //_PointsViewHorizontalBorders.First = _AxisX.AxisMinValue;
            //_PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
        }

        private async void _AxisX_AxisSizeChanged(object? sender, double e)
        {
            if (_ChartMode == ChartMode.Live)
            {
                _PointsTotal.Clear();
                _PointsView.Clear();
                //this.Points = _PointsView;


            }
            else if (_ChartMode == ChartMode.Static)
            {
                /// TODO: Add handle
            }

            //_MutexUpdateChart.WaitOne();

            //await UpdatePointsViewAsync();
            //await UpdateMarkersAsync();
            UpdateView();
            //_MutexUpdateChart.ReleaseMutex();
        }


        private async void _AxisY_AxisMinValueChanged(object? sender, double e)
        {
            _PointsViewVerticalBorders.First = _AxisY.AxisMinValue;
            _PointsViewVerticalBorders.Second = _AxisY.AxisMaxValue;
            
            //await UpdatePointsViewAsync();
            UpdateView();
        }

        private async void _AxisY_AxisSizeChanged(object? sender, double e)
        {
            //_MutexUpdateChart.WaitOne();
            //await UpdatePointsViewAsync();

            UpdateView();
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

        #region Public Methods

        public void SetParams(Canvas chartArea, HorizontalAxi axisX, VerticalAxi axisY, ChartMode chartMode = ChartMode.Live)
        {
            VerticalMarkers = new ObservableCollection<VerticalMarker>();
            VerticalMarkers.CollectionChanged += VerticalMarkers_CollectionChanged;

            _MutexUpdateChart = new Mutex(initiallyOwned: false);
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

            _PointsView = new List<Point>();
            _PointsTotal = new List<Point>();


            IsValid = true;
        }


        /*
                public async Task AddPointAsync(Point point)
                {
                    PrepareSeriesPoints(point);

                    //_MutexUpdateChart.WaitOne();
                    //if(point.X > _PointsTotalBorders.First && point.X < _PointsTotalBorders.Second)
                    await UpdatePointsViewAsync();
                    //_MutexUpdateChart.ReleaseMutex();
                }

                public async Task AddPointsRangeAsync(IEnumerable<Point> points)
                {
                    //await Task.Run(async () =>
                    //{
                    //    foreach (Point point in points)
                    //    {
                    //        await AddPointAsync(point);
                    //    }
                    //});

                    foreach (Point point in points)
                    {
                        PrepareSeriesPoints(point);
                    }
                    await UpdatePointsViewAsync();
                }
        */
        public async Task AddPointAsync(Point point)
        {
            if (_isUpdating) return; // Защита от рекурсии

            lock (_lockObj)
            {
                PrepareSeriesPoints(point);
            }

            // ✅ Обновляем только UI поток, без Task.Run
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_isUpdating) return;
                _isUpdating = true;

                try
                {
                    UpdatePointsViewFast();
                    UpdateMarkersSync();
                }
                finally
                {
                    _isUpdating = false;
                }
            });
        }

        public async Task AddPointsRangeAsync(IEnumerable<Point> points)
        {
            var pointsList = points as IList<Point> ?? points.ToList();

            lock (_lockObj)
            {
                foreach (var point in pointsList)
                {
                    PrepareSeriesPoints(point);
                }
            }

            // ✅ Одно обновление на весь пакет
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdatePointsViewFast();
                UpdateMarkersSync();
            });
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

        private void UpdatePointsViewFast()
        {
            lock (_lockObj)
            {
                this.Points.Clear(); // ✅ Очищаем, а не создаем заново
                _PointsView.Clear();

                if (_PointsTotal.Count == 0 || _ChartArea.Bounds.Width == 0)
                    return;

                PrepareCurrentScreenPointsFast();

                if (_PointsView.Count == 0)
                    return;

                double width = _ChartArea.Bounds.Width;
                double height = _ChartArea.Bounds.Height;
                double xRange = _AxisX.AxisSize;
                double yRange = _AxisY.AxisSize;
                double yMin = _PointsViewVerticalBorders.First;
                double xMin = _PointsViewHorizontalBorders.First;

                // ✅ Предварительное резервирование памяти
                //if (this.Points is PointCollection pc)
                //{
                //    // PointCollection не имеет Capacity, но Clear() сохраняет внутреннюю память
                //}

                foreach (var point in _PointsView)
                {
                    // ✅ Быстрый расчет координат
                    double x = ((point.X - xMin) / xRange) * width;
                    double y = CalculateYFast(point.Y, yMin, yRange, height);

                    this.Points.Add(new Point(x, y));
                }
            }
        }

        // ✅ Вынесенный быстрый расчет Y
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double CalculateYFast(double value, double yMin, double yRange, double height)
        {
            if (value <= yMin) return height;
            if (value >= yMin + yRange) return 0;
            return height - ((value - yMin) / yRange) * height;
        }

        // ✅ Быстрый поиск с ранним выходом
        private void PrepareCurrentScreenPointsFast()
        {
            double minX = _PointsViewHorizontalBorders.First;
            double maxX = _PointsViewHorizontalBorders.Second;

            // ✅ Начинаем с конца (там свежие точки)
            for (int i = _PointsTotal.Count - 1; i >= 0; i--)
            {
                var p = _PointsTotal[i];

                if (p.X > maxX) continue; // Пропускаем слишком правые
                if (p.X < minX) break;    // Выходим при достижении левой границы

                _PointsView.Insert(0, p); // Вставляем в начало для сохранения порядка
            }
        }

        // ✅ Безопасное обновление маркеров
        private void UpdateMarkersSync()
        {
            var markersToRemove = new List<VerticalMarker>();

            foreach (var marker in VerticalMarkers)
            {
                if (_ChartMode == ChartMode.Live && marker.Level < _PointsTotalBorders.First)
                {
                    markersToRemove.Add(marker);
                }
                else
                {
                    marker.PointsView = _PointsViewHorizontalBorders;
                    marker.UpdateMarker();
                }
            }

            // ✅ Удаляем после итерации
            foreach (var marker in markersToRemove)
            {
                VerticalMarkers.Remove(marker);
                _ChartArea.Children.Remove(marker);
            }
        }

        // ✅ Исправленная подготовка точек с ограничением
        private void PrepareSeriesPoints(Point point)
        {
            if (_PointsTotal.Count == 0)
            {
                _PointsTotalBorders.First = point.X;
                _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);
                _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
            }
            else if (_ChartMode == ChartMode.Live)
            {
                if (point.X > _PointsTotalBorders.Second)
                {
                    // ✅ Удаляем старые точки пакетом
                    int removeCount = Math.Min(100, _PointsTotal.Count);
                    if (removeCount > 0)
                    {
                        _PointsTotal.RemoveRange(0, removeCount);
                        _PointsTotalBorders.First = _PointsTotal[0].X;
                        _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);
                        _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                        _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                    }
                }
            }

            _PointsTotal.Add(point);

            // ✅ Жесткое ограничение размера
            if (_PointsTotal.Count > MAX_POINTS)
            {
                int excess = _PointsTotal.Count - MAX_POINTS;
                _PointsTotal.RemoveRange(0, excess);
                _PointsTotalBorders.First = _PointsTotal[0].X;
            }
        }

        private void UpdateView()
        {
            if (_isUpdating) return;

            Dispatcher.UIThread.Post(() =>
            {
                if (_isUpdating) return;
                _isUpdating = true;

                try
                {
                    lock (_lockObj)
                    {
                        UpdatePointsViewFast();
                        UpdateMarkersSync();
                    }
                }
                finally
                {
                    _isUpdating = false;
                }
            });
        }
        /*
                private async Task UpdatePointsViewAsync()
                {
                    await Task.Run(() =>
                    {
                        Dispatcher.UIThread.Invoke(() =>
                        {
                            this.Points = new List<Point>();
                            _PointsView.Clear();

                            if (_PointsTotal.Count == 0)
                                return;

                            PrepareCurrentScreenPoints();

                            if (_PointsView.Count == 0)
                                return;

                            foreach (Point point in _PointsView)
                            {
                                double X, Y;

                                X = ((point.X - _PointsViewHorizontalBorders.First) / _AxisX.AxisSize) * _ChartArea.Bounds.Width;// _ChartArea.Width;
                                if (point.Y <= _PointsViewVerticalBorders.First)
                                    Y = _ChartArea.Bounds.Height; //_ChartArea.Height;
                                else if (point.Y >= _PointsViewVerticalBorders.Second)
                                    Y = 0;
                                else
                                    Y = _ChartArea.Bounds.Height - (((point.Y - _PointsViewVerticalBorders.First) / _AxisY.AxisSize) * _ChartArea.Bounds.Height);//); //_ChartArea.Height);
                                this.Points.Add(new Point(X, Y));
                            }
                        });

                    });
                }

                private void PrepareCurrentScreenPoints()
                {
                    for(int i = 0; i < _PointsTotal.Count; i++)
                    {
                        if (i >= _PointsTotal.Count)
                            return;

                        if (_PointsTotal[i].X >= _PointsViewHorizontalBorders.First && _PointsTotal[i].X <= _PointsViewHorizontalBorders.Second)
                            _PointsView.Add(_PointsTotal[i]);
                    }
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
                            VerticalMarkers[i].PointsView = _PointsViewHorizontalBorders;
                            VerticalMarkers[i].UpdateMarker();
                        }
                    });

                }

                private void PrepareSeriesPoints(Point point)
                {
                    if (_PointsTotal.Count == 0)
                    {
                        _PointsTotalBorders.First = point.X;
                        _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);

                        _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                        _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                        //await UpdateMarkersAsync();
                    }
                    else
                    {
                        if (_ChartMode == ChartMode.Live)
                        {

                            if (point.X > _PointsTotalBorders.Second)
                            {
                                double delta = _PointsTotal[1].X - _PointsTotal[0].X;
                                _PointsTotal.RemoveAt(0);
                                _PointsTotalBorders.First = _PointsTotal[0].X;
                                _PointsTotalBorders.Second = _PointsTotalBorders.First + (_AxisX.AxisSize * 3);
                                //_AxisX.SetAxisMinValue(_AxisX.AxisMinValue + delta);
                                _PointsViewHorizontalBorders.First = _PointsTotalBorders.First + _AxisX.AxisMinValue;
                                _PointsViewHorizontalBorders.Second = _PointsViewHorizontalBorders.First + _AxisX.AxisSize;
                                //await UpdateMarkersAsync();
                            }

                        }
                        else if (_ChartMode == ChartMode.Static)
                        {
                            /// TODO: Add handle
                        }


                    }
                    _PointsTotal.Add(point);
                }
        */

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
