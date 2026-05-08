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
                _PointsView.Clear();
                //this.Points = _PointsView;


            }
            else if (_ChartMode == ChartMode.Static)
            {
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
            for (int i = 0; i < _PointsTotal.Count; i++)
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
