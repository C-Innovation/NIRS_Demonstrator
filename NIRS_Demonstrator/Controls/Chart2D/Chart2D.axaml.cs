using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace NIRS_Demonstrator;

public partial class Chart2D : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<IBrush> ChartBackgroundProperty =
         AvaloniaProperty.Register<Chart2D, IBrush>(nameof(ChartBackground), defaultValue: Brushes.Transparent);

    /// <summary>
    /// Main chart background
    /// </summary>
    public IBrush ChartBackground
    {
        get { return (IBrush)GetValue(ChartBackgroundProperty); }
        set { SetValue(ChartBackgroundProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == ChartBackgroundProperty)
        {
            MainGrid.Background = (IBrush?)change.NewValue;
        }


        base.OnPropertyChanged(change);

    }

    #endregion

    #region Private Members

    private Point _LastPointerPosition;
    private bool _IsPointerPressed;

    #endregion

    #region Public Properties

    public VerticalAxi AxisY { get; set; }
    public HorizontalAxi AxisX { get; set; }

    public ObservableCollection<Series> ChartSeries { get; set; }

    public ObservableCollection<HorizontalMarker> HorizontalMarkers { get; set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Default Constructor
    /// </summary>
    public Chart2D()
    {
        InitializeComponent();

        ChartSeries = new ObservableCollection<Series>();
        ChartSeries.CollectionChanged += Series_CollectionChanged;

        HorizontalMarkers = new ObservableCollection<HorizontalMarker>();
        HorizontalMarkers.CollectionChanged += HorizontalMarkers_CollectionChanged;
        AxisY = new VerticalAxi(ChartArea);
        AxisX = new HorizontalAxi(ChartArea);

        HorizontalScroll.Minimum = 0;
        HorizontalScroll.Maximum = 2000;
        HorizontalScroll.Value = 1000;
        HorizontalScroll.ValueChanged += HorizontalScroll_ValueChanged;

        VerticalScroll.Minimum = -4000;
        VerticalScroll.Maximum = 4000;
        VerticalScroll.Value = 0;
        VerticalScroll.ValueChanged += VerticalScroll_ValueChanged;
        // Modules initialization
        Initialize();

        // On loaded handling
        this.Loaded += Chart2D_Loaded;

        ChartArea.PointerMoved += ChartArea_PointerMoved;
        ChartArea.PointerPressed += ChartArea_PointerPressed;
        ChartArea.PointerReleased += ChartArea_PointerReleased;
        // Size changed handling
        //ChartArea.SizeChanged += ChartArea_SizeChanged;
    }

   

    private void ChartArea_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        _IsPointerPressed = false;
        _LastPointerPosition = new Point(0,0);
    }

    private void ChartArea_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        _LastPointerPosition = e.GetPosition(sender as Canvas);
        _IsPointerPressed = true;
        

    }

    
    private void ChartArea_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
         
        if (!_IsPointerPressed)
            return;

        var chartArea = sender as Canvas;

        if (chartArea is null)
            return;

        var currentPosition = e.GetPosition(chartArea);

        var offsetX = currentPosition.X - _LastPointerPosition.X;
        var offsetY = currentPosition.Y - _LastPointerPosition.Y;

        _LastPointerPosition = new Point(currentPosition.X, currentPosition.Y);

        var offsetXpt = (offsetX / chartArea.Bounds.Width) * AxisX.AxisSize;
        var offsetYpt = (offsetY / chartArea.Bounds.Height) * AxisY.AxisSize;

        if (HorizontalScroll.Value - offsetXpt >= HorizontalScroll.Maximum)
            HorizontalScroll.Value = HorizontalScroll.Maximum;
 
        else if (HorizontalScroll.Value - offsetXpt <= HorizontalScroll.Minimum)
            HorizontalScroll.Value = HorizontalScroll.Minimum;
            
        else
            HorizontalScroll.Value -= offsetXpt;


        if (VerticalScroll.Value - offsetYpt >= VerticalScroll.Maximum)
            VerticalScroll.Value = VerticalScroll.Maximum;

        else if (VerticalScroll.Value - offsetYpt <= VerticalScroll.Minimum)
            VerticalScroll.Value = VerticalScroll.Minimum;

        else
            VerticalScroll.Value -= offsetYpt;
    }

    #endregion

    #region Private Callbacks

    private void Chart2D_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //_MajorGrid.UpdatePositions(new Size(ChartArea.Bounds.Width, ChartArea.Bounds.Height));
        AxisY.Update(new Size(ChartArea.Bounds.Width, ChartArea.Bounds.Height));
        AxisX.Update(new Size(ChartArea.Bounds.Width, ChartArea.Bounds.Height));
    }

    private void Series_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var item in e.NewItems)
                {
                    if (item is Series series)
                        ChartArea.Children.Add(series);
                }
                break;
            
            case NotifyCollectionChangedAction.Remove:
                foreach(var item in e.OldItems)
                {
                    if (item is Series series)
                        ChartArea.Children.Remove(series);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (var item in e.OldItems)
                {
                    if (item is Series series)
                        ChartArea.Children.Remove(series);
                }
                foreach (var item in e.NewItems)
                {
                    if (item is Series series)
                        ChartArea.Children.Add(series);
                }
                break;

            case NotifyCollectionChangedAction.Reset:

                ChartSeries.Clear();
                ChartSeries = new ObservableCollection<Series>();
                break;

            default: break;
        }

        foreach (Series series in ChartSeries)
        {
            if (!series.IsValid)
                series.SetParams(ChartArea, AxisX, AxisY);
        }
    }

    private void HorizontalMarkers_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (var item in e.NewItems)
                {
                    if (item is HorizontalMarker marker)
                        ChartArea.Children.Add(marker);
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (var item in e.OldItems)
                {
                    if (item is HorizontalMarker marker)
                        ChartArea.Children.Remove(marker);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (var item in e.OldItems)
                {
                    if (item is HorizontalMarker marker)
                        ChartArea.Children.Remove(marker);
                }
                foreach (var item in e.NewItems)
                {
                    if (item is HorizontalMarker marker)
                        ChartArea.Children.Add(marker);
                }
                break;

            case NotifyCollectionChangedAction.Reset:

                HorizontalMarkers.Clear();
                HorizontalMarkers = new ObservableCollection<HorizontalMarker>();
                break;

            default: break;
        }

        foreach (HorizontalMarker marker in HorizontalMarkers)
        {
            if (!marker.IsValid)
                marker.SetParams(ChartArea, AxisY);
        }
    }

    private void HorizontalScroll_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        AxisX.SetAxisMinValue(e.NewValue);
    }

    private void VerticalScroll_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        AxisY.SetAxisOffsetValue(e.NewValue);
    }

    //private void ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
    //{

    //}

    #endregion

    #region Public Methods

    public void UpdateChartArea()
    {
        AxisX.SetAxisSize(1000);
        HorizontalScroll.Value = 1000;
    }

    public void SetAxisXSize(double size)
    {
        AxisX.SetAxisSize(size);
        HorizontalScroll.Maximum = size*2;
        if (HorizontalScroll.Value != HorizontalScroll.Maximum / 2)
            HorizontalScroll.Value = HorizontalScroll.Maximum / 2;
        else
            AxisX.SetAxisMinValue(HorizontalScroll.Maximum / 2);
    }

    #endregion

    #region Private Methods

    private void Initialize()
    {
        
        
    }

    #endregion
}