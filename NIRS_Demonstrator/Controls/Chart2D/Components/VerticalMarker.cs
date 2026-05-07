using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;


namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class VerticalMarker : Line
    {
        #region Styled Properties
        public static readonly StyledProperty<double> LevelProperty =
         AvaloniaProperty.Register<VerticalMarker, double>(nameof(Level), defaultValue: 0.0);

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
        private HorizontalAxi _HorizontalAxi;
        #endregion

        #region Public Properties
        internal bool IsValid = false;
        internal Pair<double, double> PointsView;
        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public VerticalMarker()
        {
            IsValid = false;
        }

        public VerticalMarker(Canvas chartArea, HorizontalAxi axisX)
        {
            SetParams(chartArea, axisX);
        }

        #endregion

        #region Private Callbacks

        private void _HorizontalAxi_AxisSizeChanged(object? sender, double e)
        {
            //throw new System.NotImplementedException();
        }

        private void _HorizontalAxi_AxisMinValueChanged(object? sender, double e)
        {
            //throw new System.NotImplementedException();
        }

        private void _ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        internal void SetParams(Canvas chartArea, HorizontalAxi axisX)
        {

            _ChartArea = chartArea;
            _ChartArea.SizeChanged += _ChartArea_SizeChanged; ;
            _HorizontalAxi = axisX;
            _HorizontalAxi.AxisMinValueChanged += _HorizontalAxi_AxisMinValueChanged; ;
            _HorizontalAxi.AxisSizeChanged += _HorizontalAxi_AxisSizeChanged; ;
            this.StartPoint = new Avalonia.Point(0, 0);
            this.EndPoint = new Avalonia.Point(_ChartArea.Bounds.Width, 0);
            this.StrokeThickness = 2;
            this.Opacity = 0.75;

            //UpdateMarker();

            IsValid = true;
        }

        internal void UpdateMarker()
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (Level < PointsView.First || Level > PointsView.Second)
                {
                    this.IsVisible = false;
                    return;
                }


                double Y1 = 0;
                double X = ((Level - PointsView.First) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                double Y2 = _ChartArea.Bounds.Height;

                this.StartPoint = new Avalonia.Point(X, Y1);
                this.EndPoint = new Avalonia.Point(X, Y2);
                this.IsVisible = true;
            });
            


        }
        #endregion

        #region Private Methods



        #endregion
    }
}
