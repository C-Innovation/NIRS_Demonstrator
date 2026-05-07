using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class VerticalAxi
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private Canvas _ChartArea;
        private int _MajorGridTicks;
        private List<HorizontalGrid> _MajorGrid;
        private List<HorizontalGrid> _MinorGrid;
        #endregion

        #region Public Properties

        public double AxisSize { get; private set; } = 12;
        public double AxisTotalSize { get; private set; } = 24;
        public double AxisTickSize { get; private set; }
        public double AxisOffsetValue { get; private set; } = 0;
        public int AxisTicksCount { get; private set; }
        public double AxisMinValue { get; private set; } = -6;
        public double AxisMaxValue { get; private set; } = 6;

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
        public VerticalAxi(Canvas chartArea, int majorTicks = 6)
        {
            _ChartArea = chartArea;
            _MajorGridTicks = majorTicks - 1;
            _MajorGrid = new List<HorizontalGrid>(_MajorGridTicks);
            AxisTicksCount = majorTicks;

            _MajorGrid.Clear();
            for (int i = 0; i < _MajorGridTicks; i++)
            {
                HorizontalGrid grid = new HorizontalGrid();
                _MajorGrid.Add(grid);
            }

            foreach (HorizontalGrid grid in _MajorGrid)
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
            this.AxisTickSize = axisSize / (double)(this.AxisTicksCount);
            this.AxisMaxValue = this.AxisMinValue + axisSize;
            AxisSizeChanged?.Invoke(this, this.AxisSize);
            AxisTickSizeChanged?.Invoke(this, this.AxisTickSize);
        }

        public void SetAxisOffsetValue(double offsetValue)
        {
            this.AxisOffsetValue = offsetValue;
            this.AxisMinValue = -(this.AxisSize / 2) - this.AxisOffsetValue;
            this.AxisMaxValue = this.AxisMinValue + this.AxisSize;
            this.AxisMinValueChanged?.Invoke(this, this.AxisMinValue);
        }

        #endregion

        #region Private Methods

        #endregion
    }
}
