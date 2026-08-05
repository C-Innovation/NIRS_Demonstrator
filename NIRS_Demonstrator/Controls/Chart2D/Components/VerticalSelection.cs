using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Threading;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class VerticalSelection : Rectangle
    {
        #region Dependency Properties
        public static readonly StyledProperty<double> LevelStartProperty =
         AvaloniaProperty.Register<VerticalMarker, double>(nameof(LevelStart), defaultValue: 0.0);

        /// <summary>
        /// Main chart background
        /// </summary>
        public double LevelStart
        {
            get { return (double)GetValue(LevelStartProperty); }
            set { SetValue(LevelStartProperty, value); }
        }

        public static readonly StyledProperty<double> LevelStopProperty =
        AvaloniaProperty.Register<VerticalMarker, double>(nameof(LevelStop), defaultValue: 0.0);

        /// <summary>
        /// Main chart background
        /// </summary>
        public double LevelStop
        {
            get { return (double)GetValue(LevelStopProperty); }
            set { SetValue(LevelStopProperty, value); }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change == null) return;

            if (change.Property == LevelStartProperty || change.Property == LevelStopProperty)
            {
                Update();
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
        public VerticalSelection()
        {
            IsValid = false;
        }


        /// <summary>
        /// Extended constructor
        /// </summary>
        /// <param name="chartArea"></param>
        /// <param name="axisX"></param>
        public VerticalSelection(Canvas chartArea, HorizontalAxi axisX)
        {
            SetParams(chartArea, axisX);
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        internal void SetParams(Canvas chartArea, HorizontalAxi axisX)
        {

            _ChartArea = chartArea;
            //_ChartArea.SizeChanged += _ChartArea_SizeChanged; ;
            _HorizontalAxi = axisX;
            //_HorizontalAxi.AxisMinValueChanged += _HorizontalAxi_AxisMinValueChanged; ;
            //_HorizontalAxi.AxisSizeChanged += _HorizontalAxi_AxisSizeChanged; ;
            this.SetValue(Canvas.LeftProperty, 0);
            this.SetValue(Canvas.TopProperty, 0);
            this.Height = 0;
            this.Width = 0;
            this.StrokeThickness = 1;
            this.Opacity = 0.5;
            // Прямоугольник выделения — декорация: он не должен перехватывать
            // нажатия у области построения, иначе следующее выделение или
            // отмена по ней не сработают.
            this.IsHitTestVisible = false;

            //UpdateMarker();

            IsValid = true;
        }

        /// <summary>
        /// Задаёт границы выделения и окно просмотра одной операцией, без
        /// промежуточной перерисовки на каждое из свойств.
        /// </summary>
        internal void SetRange(double levelStart, double levelStop, Pair<double, double> pointsView)
        {
            if (levelStart > levelStop)
                (levelStart, levelStop) = (levelStop, levelStart);

            PointsView = pointsView;
            LevelStart = levelStart;
            LevelStop = levelStop;
            Update();
        }

        internal void Update()
        {
            double Y1 = 0;
            double X = 0;
            double Y2 = 0;
            double W = 0;
            Dispatcher.UIThread.Invoke(() =>
            {
                if (LevelStart > PointsView.Second || LevelStop < PointsView.First)
                {
                    this.IsVisible = false;
                    return;
                }
                else if (LevelStart < PointsView.First && LevelStop > PointsView.First)
                {
                    Y1 = 0;
                    X = 0; 
                    Y2 = _ChartArea.Bounds.Height;
                    W = ((LevelStop - PointsView.First) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                }
                else if (LevelStop > PointsView.Second && LevelStart < PointsView.Second)
                {
                    Y1 = 0;
                    X = ((LevelStart - PointsView.First) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                    Y2 = _ChartArea.Bounds.Height;
                    W = ((PointsView.Second - LevelStart) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                }
                else
                {
                    Y1 = 0;
                    X = ((LevelStart - PointsView.First) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                    Y2 = _ChartArea.Bounds.Height;
                    W = ((LevelStop - LevelStart) / _HorizontalAxi.AxisSize) * _ChartArea.Bounds.Width;
                    
                }
                if (W < 0)
                    return;
                if (W == 0 || W == 1)
                {
                    W = 1;
                    Opacity = 1;
                }
                else
                {
                    Opacity = 0.5;
                }
                this.SetValue(Canvas.LeftProperty, X);
                this.SetValue(Canvas.TopProperty, 0);
                this.Height = Y2;
                this.Width = W;
                this.IsVisible = true;
                
            });



        }

        internal void Clear()
        {
            this.SetValue(Canvas.LeftProperty, 0);
            this.SetValue(Canvas.TopProperty, 0);
            this.Height = 0;
            this.Width = 0;
            this.IsVisible = false;
        }
        #endregion

        #region Private Methods

        #endregion
    }
}
