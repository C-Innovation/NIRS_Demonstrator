using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class HorizontalAxi
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private Canvas _ChartArea;
        private int _MajorGridTicks;
        private List<VerticalGrid> _MajorGrid;
        private List<VerticalGrid> _MinorGrid;
        #endregion

        #region Public Properties

        public double AxisSize { get; private set; } = 1000;
        public double AxisTickSize { get; private set; }
        public int AxisTicksCount { get; private set; }
        public double AxisMinValue { get; private set; } = 1000;
        public double AxisMaxValue { get; private set; } = 2000;

        #endregion

        #region Public Commands

        #endregion

        #region Public Events
        public event EventHandler<double> AxisTickSizeChanged;
        public event EventHandler<double> AxisSizeChanged;
        public event EventHandler<double> AxisMinValueChanged;
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public HorizontalAxi(Canvas chartArea, int majorTicks = 10)
        {
            _ChartArea = chartArea;
            _MajorGridTicks = majorTicks - 1;
            AxisTicksCount = majorTicks;
            AxisTickSize = AxisSize / AxisTicksCount;
            _MajorGrid = new List<VerticalGrid>(_MajorGridTicks);

            _MajorGrid.Clear();
            for (int i = 0; i < _MajorGridTicks; i++)
            {
                VerticalGrid grid = new VerticalGrid();
                _MajorGrid.Add(grid);
            }

            foreach (VerticalGrid grid in _MajorGrid)
                _ChartArea.Children.Add(grid);

            _ChartArea.SizeChanged += ChartArea_SizeChanged;
        }

        #endregion

        #region Private Callbacks
        private void ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            _MajorGrid.UpdatePositions(e.NewSize);
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        public void Update(Size chartAreaSize)
        {
            _MajorGrid.UpdatePositions(chartAreaSize);
        }

        public void SetAxisSize(double axisSize) 
        {
            this.AxisSize = axisSize;
            this.AxisTickSize = axisSize /(double )(_MajorGridTicks + 1);
            this.AxisMinValue = 0;
            this.AxisMaxValue =  this.AxisMinValue + this.AxisSize;
            AxisSizeChanged?.Invoke(this, this.AxisSize);
            AxisTickSizeChanged?.Invoke(this, this.AxisTickSize);

        }

        public void SetAxisMinValue(double minValue)
        {
            this.AxisMinValue = minValue;
            this.AxisMaxValue = this.AxisMinValue + this.AxisSize;
            this.AxisMinValueChanged?.Invoke(this, this.AxisMinValue);
        }
        #endregion



        #region Private Methods

        #endregion
    }
}
