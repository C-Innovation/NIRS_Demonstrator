using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class HorizontalMarker : Line
    {
        #region Styled Properties
        public static readonly StyledProperty<double> LevelProperty =
         AvaloniaProperty.Register<HorizontalMarker, double>(nameof(Level), defaultValue: 0.0);

        /// <summary>
        /// Main chart background
        /// </summary>
        public double Level
        {
            get { return (double)GetValue(LevelProperty); }
            set { SetValue(LevelProperty, value); }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change == null) return;

            if (change.Property == LevelProperty)
            {
                UpdateMarker();
            }


            base.OnPropertyChanged(change);

        }
        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private Canvas _ChartArea;
        private VerticalAxi _VerticalAxi;

        #endregion

        #region Public Properties
        internal bool IsValid = false;
        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        public HorizontalMarker()
        {
             IsValid = false;       
        }

        /// <summary>
        /// Extended constructor
        /// </summary>
        public HorizontalMarker(Canvas chartArea, VerticalAxi axisY)
        {
            SetParams(chartArea, axisY);

        }
               

        #endregion

        #region Private Callbacks

        private void _ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateMarker();
        }

        private void _VerticalAxi_AxisMinValueChanged(object? sender, double e)
        {
            UpdateMarker();
        }

        private void _VerticalAxi_AxisSizeChanged(object? sender, double e)
        {
            UpdateMarker();
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        internal void SetParams(Canvas chartArea, VerticalAxi axisY)
        {

            _ChartArea = chartArea;
            _ChartArea.SizeChanged += _ChartArea_SizeChanged;
            _VerticalAxi = axisY;
            _VerticalAxi.AxisMinValueChanged += _VerticalAxi_AxisMinValueChanged;
            _VerticalAxi.AxisSizeChanged += _VerticalAxi_AxisSizeChanged;
            this.StartPoint = new Avalonia.Point(0, 0);
            this.EndPoint = new Avalonia.Point(_ChartArea.Bounds.Width, 0);
            this.StrokeThickness = 2;
            this.Opacity = 0.75;
            UpdateMarker();
            
            IsValid = true;
        }

        #endregion

        #region Private Methods

        private void UpdateMarker()
        {
            if (_ChartArea == null)
                return;

            if (Level > _VerticalAxi.AxisMinValue && Level < _VerticalAxi.AxisMaxValue)
            {

                double X1 = 0;
                double Y = _ChartArea.Bounds.Height - (((Level - _VerticalAxi.AxisMinValue) / _VerticalAxi.AxisSize) * _ChartArea.Bounds.Height);
                double X2 = _ChartArea.Bounds.Width;
                
                this.StartPoint = new Avalonia.Point(X1, Y);
                this.EndPoint = new Avalonia.Point(X2, Y);
                this.IsVisible = true;
            }
            else
            {
                this.IsVisible = false;
            }
            
        }

        #endregion
    }
}
