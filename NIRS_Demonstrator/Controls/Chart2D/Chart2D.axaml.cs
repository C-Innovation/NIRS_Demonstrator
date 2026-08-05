using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
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

    public static readonly StyledProperty<IBrush> SelectionBrushProperty =
        AvaloniaProperty.Register<Chart2D, IBrush>(
            nameof(SelectionBrush),
            defaultValue: new SolidColorBrush(Color.FromRgb(0x1E, 0x90, 0xFF)));

    /// <summary>
    /// Заливка прямоугольников выделения области.
    /// </summary>
    public IBrush SelectionBrush
    {
        get { return (IBrush)GetValue(SelectionBrushProperty); }
        set { SetValue(SelectionBrushProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == ChartBackgroundProperty)
        {
            MainGrid.Background = (IBrush?)change.NewValue;
        }
        else if (change.Property == SelectionBrushProperty)
        {
            IBrush? brush = (IBrush?)change.NewValue;

            foreach (VerticalSelection selection in _Selections)
                selection.Fill = brush;

            if (_PendingSelection != null)
                _PendingSelection.Fill = brush;
        }


        base.OnPropertyChanged(change);

    }

    #endregion

    #region Private Members

    private Point _LastPointerPosition;
    private bool _IsPanning;
    private ChartMode _ChartMode = ChartMode.Live;

    /// <summary>
    /// Подтверждённые выделенные области. Прямоугольник на каждую область.
    /// </summary>
    private readonly List<VerticalSelection> _Selections = new List<VerticalSelection>();

    /// <summary>
    /// Прямоугольник, показываемый пока правая кнопка удерживается.
    /// </summary>
    private VerticalSelection _PendingSelection;

    private bool _IsSelecting;
    private double _SelectionStartValue;
    //protected ObservableCollection<HorizontalMarker> _HorizontalMarkers;
    #endregion

    #region Public Properties

    public VerticalAxi AxisY { get; set; }
    public HorizontalAxi AxisX { get; set; }

    public ObservableCollection<Series> ChartSeries { get; set; }

    public ChartMode ChartMode 
    { 
        get => _ChartMode; 
        set => _ChartMode = value; 
    }

    /// <summary>
    /// Разрешено ли выделение области по горизонтали правой кнопкой мыши.
    /// </summary>
    public bool IsSelectionEnabled { get; set; } = true;

    /// <summary>
    /// Минимальная ширина выделения в пикселях. Более узкое протягивание
    /// считается промахом и выделение не создаёт.
    /// </summary>
    public double MinSelectionWidth { get; set; } = 3;

    /// <summary>
    /// Диапазоны подтверждённых выделенных областей в реальных значениях оси X.
    /// </summary>
    public IReadOnlyList<Pair<double, double>> SelectionRanges
    {
        get
        {
            List<Pair<double, double>> ranges = new List<Pair<double, double>>(_Selections.Count);

            foreach (VerticalSelection selection in _Selections)
                ranges.Add(new Pair<double, double>(selection.LevelStart, selection.LevelStop));

            return ranges;
        }
    }

    public ObservableCollection<HorizontalMarker> HorizontalMarkers { get; set; }
    //{ 
    //    get => _HorizontalMarkers; 
    //    set
    //    {
    //        //if (_HorizontalMarkers != null)
    //        //{
    //            _HorizontalMarkers = value;
    //        //}
    //    }
    //}

    
    #endregion

    #region Public Events

    public event EventHandler<double> OnHorizontalScrollValueChanged;
    public event EventHandler<Point> OnChartAreaDoubleClick;

    /// <summary>
    /// Границы выделения меняются: правая кнопка удерживается и указатель движется.
    /// </summary>
    public event EventHandler<ChartSelectionRangeEventArgs> SelectionChanging;

    /// <summary>
    /// Выделение завершено: правая кнопка отпущена, ширина области достаточна.
    /// </summary>
    public event EventHandler<ChartSelectionRangeEventArgs> SelectionCompleted;

    /// <summary>
    /// Запрошено снятие одной выделенной области: двойной клик правой кнопкой
    /// по ней. Передаётся её индекс в порядке добавления.
    /// </summary>
    public event EventHandler<int> SelectionRemoveRequested;
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

        VerticalScroll.Minimum = -3;
        VerticalScroll.Maximum = 3;
        VerticalScroll.Value = 0;
        VerticalScroll.ValueChanged += VerticalScroll_ValueChanged;
        // Modules initialization
        Initialize();

        // On loaded handling
        this.Loaded += Chart2D_Loaded;

        ChartArea.PointerMoved += ChartArea_PointerMoved;
        ChartArea.PointerPressed += ChartArea_PointerPressed;
        ChartArea.PointerReleased += ChartArea_PointerReleased;
        ChartArea.PointerCaptureLost += ChartArea_PointerCaptureLost;

        // Выделение задано в реальных значениях, поэтому при прокрутке,
        // изменении масштаба и размера области его нужно перерисовать.
        ChartArea.SizeChanged += ChartArea_SizeChanged;
        AxisX.AxisMinValueChanged += AxisX_SelectionAffectingChanged;
        AxisX.AxisSizeChanged += AxisX_SelectionAffectingChanged;
    }

   

    private void ChartArea_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
    {
        if (_IsSelecting && e.InitialPressMouseButton == MouseButton.Right)
        {
            _IsSelecting = false;
            e.Pointer.Capture(null);
            HidePendingSelection();

            double stopValue = SelectionValueAt(e.GetPosition(ChartArea).X);
            ChartSelectionRangeEventArgs range = new ChartSelectionRangeEventArgs(_SelectionStartValue, stopValue);

            // Случайный щелчок правой кнопкой не должен плодить выделения нулевой ширины.
            if (ValueToCanvasX(range.StopValue) - ValueToCanvasX(range.StartValue) >= MinSelectionWidth)
                SelectionCompleted?.Invoke(this, range);

            e.Handled = true;
            return;
        }

        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _IsPanning = false;
            _LastPointerPosition = new Point(0, 0);
        }
    }

    private void ChartArea_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var chartArea = sender as Canvas;

        if (chartArea is null)
            return;

        var properties = e.GetCurrentPoint(chartArea).Properties;
        var position = e.GetPosition(chartArea);

        if (properties.IsRightButtonPressed)
        {
            if (!IsSelectionEnabled)
                return;

            if (e.ClickCount >= 2)
            {
                _IsSelecting = false;
                e.Pointer.Capture(null);
                HidePendingSelection();

                // Двойной клик по выделенной области снимает именно её.
                // Все области сразу снимаются только кнопкой на странице.
                int index = GetSelectionIndexAt(position.X);

                if (index >= 0)
                    SelectionRemoveRequested?.Invoke(this, index);

                e.Handled = true;
                return;
            }

            _IsSelecting = true;
            _SelectionStartValue = SelectionValueAt(position.X);
            // Захват указателя нужен, чтобы протягивание за пределы области
            // построения не обрывалось на середине.
            e.Pointer.Capture(chartArea);
            ShowPendingSelection(_SelectionStartValue, _SelectionStartValue);
            SelectionChanging?.Invoke(this, new ChartSelectionRangeEventArgs(_SelectionStartValue, _SelectionStartValue));
            e.Handled = true;
            return;
        }

        if (!properties.IsLeftButtonPressed)
            return;

        _LastPointerPosition = position;
        _IsPanning = true;
    }


    private void ChartArea_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        var chartArea = sender as Canvas;

        if (chartArea is null)
            return;

        if (_IsSelecting)
        {
            double currentValue = SelectionValueAt(e.GetPosition(chartArea).X);
            ChartSelectionRangeEventArgs range = new ChartSelectionRangeEventArgs(_SelectionStartValue, currentValue);

            ShowPendingSelection(range.StartValue, range.StopValue);
            SelectionChanging?.Invoke(this, range);
            return;
        }

        if (!_IsPanning)
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

        ChartArea.DoubleTapped += ChartArea_DoubleTapped;
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
                        RemoveSeries(series);
                }
                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (var item in e.OldItems)
                {
                    if (item is Series series)
                        RemoveSeries(series);
                }
                foreach (var item in e.NewItems)
                {
                    if (item is Series series)
                        ChartArea.Children.Add(series);
                }
                break;

            case NotifyCollectionChangedAction.Reset:

                // Reset не сообщает, что именно было удалено, поэтому убираем с холста
                // все серии, которых больше нет в коллекции.
                for (int i = ChartArea.Children.Count - 1; i >= 0; i--)
                {
                    if (ChartArea.Children[i] is Series series && !ChartSeries.Contains(series))
                        RemoveSeries(series);
                }
                break;

            default: break;
        }

        foreach (Series series in ChartSeries)
        {
            if (!series.IsValid)
                series.SetParams(ChartArea, AxisX, AxisY, _ChartMode);
        }
    }

    /// <summary>
    /// Убирает серию с холста и отписывает её от области построения и осей,
    /// иначе она остаётся достижимой из них вместе со всеми накопленными точками.
    /// </summary>
    private void RemoveSeries(Series series)
    {
        ChartArea.Children.Remove(series);
        series.Dispose();
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
                throw new NotImplementedException(nameof(e));
                //foreach (var item in HorizontalMarkers)
                //{
                //    if (item is HorizontalMarker marker)
                //        ChartArea.Children.Remove(marker);
                //}
                //_HorizontalMarkers.Clear();
                //_HorizontalMarkers = new ObservableCollection<HorizontalMarker>();
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
        OnHorizontalScrollValueChanged?.Invoke(this, e.NewValue);
    }

    private void VerticalScroll_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        AxisY.SetAxisOffsetValue(e.NewValue);
    }

    private void ChartArea_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        var chartArea = sender as Canvas;

        if (chartArea is null)
            return;

        var currentPosition = e.GetPosition(chartArea);

        double x = AxisX.AxisMinValue + ((currentPosition.X / chartArea.Bounds.Width) * AxisX.AxisSize);
        double y = AxisY.AxisMinValue + (((chartArea.Bounds.Height - currentPosition.Y) / chartArea.Bounds.Height) * AxisY.AxisSize);

        OnChartAreaDoubleClick?.Invoke(this, new Point(x, y));
    }

    private void ChartArea_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        RefreshSelections();
    }

    private void AxisX_SelectionAffectingChanged(object? sender, double e)
    {
        RefreshSelections();
    }

    /// <summary>
    /// Захват указателя может быть потерян (например, окно ушло из фокуса) —
    /// незавершённое выделение в этом случае просто сбрасывается.
    /// </summary>
    private void ChartArea_PointerCaptureLost(object? sender, Avalonia.Input.PointerCaptureLostEventArgs e)
    {
        if (!_IsSelecting)
            return;

        _IsSelecting = false;
        HidePendingSelection();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Координата X на области построения -> реальное значение оси X.
    /// </summary>
    public double CanvasXToValue(double canvasX)
    {
        double width = ChartArea.Bounds.Width;

        if (width <= 0)
            return AxisX.AxisMinValue;

        return AxisX.AxisMinValue + ((canvasX / width) * AxisX.AxisSize);
    }

    /// <summary>
    /// Реальное значение оси X -> координата X на области построения.
    /// </summary>
    public double ValueToCanvasX(double value)
    {
        if (AxisX.AxisSize <= 0)
            return 0;

        return ((value - AxisX.AxisMinValue) / AxisX.AxisSize) * ChartArea.Bounds.Width;
    }

    /// <summary>
    /// Добавляет подтверждённую выделенную область на всю высоту оси Y.
    /// </summary>
    public void AddSelection(double startValue, double stopValue)
    {
        VerticalSelection selection = new VerticalSelection(ChartArea, AxisX);
        selection.Fill = SelectionBrush;
        ChartArea.Children.Add(selection);
        _Selections.Add(selection);

        selection.SetRange(startValue, stopValue, GetSelectionPointsView());
    }

    /// <summary>
    /// Индекс выделенной области под указанной координатой X области построения.
    /// Возвращает -1, если под ней выделения нет. Из перекрывающихся областей
    /// выбирается добавленная последней — та, что нарисована сверху.
    /// </summary>
    public int GetSelectionIndexAt(double canvasX)
    {
        double value = CanvasXToValue(canvasX);

        for (int i = _Selections.Count - 1; i >= 0; i--)
        {
            VerticalSelection selection = _Selections[i];

            if (value >= selection.LevelStart && value <= selection.LevelStop)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Убирает одну выделенную область по её индексу в порядке добавления.
    /// </summary>
    public void RemoveSelectionAt(int index)
    {
        if ((uint)index >= (uint)_Selections.Count)
            return;

        ChartArea.Children.Remove(_Selections[index]);
        _Selections.RemoveAt(index);
    }

    /// <summary>
    /// Убирает все выделенные области, включая незавершённую.
    /// </summary>
    public void ClearSelections()
    {
        HidePendingSelection();

        foreach (VerticalSelection selection in _Selections)
            ChartArea.Children.Remove(selection);

        _Selections.Clear();
    }

    /// <summary>
    /// Показывает границы ещё не подтверждённого выделения.
    /// </summary>
    public void ShowPendingSelection(double startValue, double stopValue)
    {
        if (_PendingSelection == null)
        {
            _PendingSelection = new VerticalSelection(ChartArea, AxisX);
            _PendingSelection.Fill = SelectionBrush;
            ChartArea.Children.Add(_PendingSelection);
        }

        _PendingSelection.SetRange(startValue, stopValue, GetSelectionPointsView());
    }

    /// <summary>
    /// Скрывает незавершённое выделение.
    /// </summary>
    public void HidePendingSelection()
    {
        if (_PendingSelection == null)
            return;

        ChartArea.Children.Remove(_PendingSelection);
        _PendingSelection = null;
    }

    /// <summary>
    /// Пересчитывает положение всех выделенных областей под текущее окно просмотра.
    /// </summary>
    public void RefreshSelections()
    {
        if (_Selections.Count == 0 && _PendingSelection == null)
            return;

        Pair<double, double> pointsView = GetSelectionPointsView();

        foreach (VerticalSelection selection in _Selections)
        {
            selection.PointsView = pointsView;
            selection.Update();
        }

        if (_PendingSelection != null)
        {
            _PendingSelection.PointsView = pointsView;
            _PendingSelection.Update();
        }
    }

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

    public void SetAxisXSizeForViewer(double size, double totalSize)
    {
        AxisX.SetAxisSize(size);
        if (totalSize > size)
        {
            HorizontalScroll.Value = 0;
            HorizontalScroll.Maximum = totalSize - size;
        }
        else
        {
            HorizontalScroll.Value = 0;
            HorizontalScroll.Maximum = 0;
        }
        //HorizontalScroll.Maximum = size * 2;
        //if (HorizontalScroll.Value != HorizontalScroll.Maximum / 2)
        //    HorizontalScroll.Value = HorizontalScroll.Maximum / 2;
        //else
        //    AxisX.SetAxisMinValue(HorizontalScroll.Maximum / 2);
    }

    #endregion

    #region Private Methods

    private void Initialize()
    {


    }

    /// <summary>
    /// Окно просмотра в реальных значениях оси X — в нём прямоугольники
    /// выделения считают своё положение на области построения.
    /// </summary>
    private Pair<double, double> GetSelectionPointsView()
    {
        return new Pair<double, double>(AxisX.AxisMinValue, AxisX.AxisMinValue + AxisX.AxisSize);
    }

    /// <summary>
    /// Реальное значение под указателем, прижатое к границам видимого окна:
    /// при захвате указателя протягивание уходит за пределы области построения,
    /// а выделять что-то вне видимого диапазона смысла нет.
    /// </summary>
    private double SelectionValueAt(double canvasX)
    {
        double value = CanvasXToValue(canvasX);

        if (value < AxisX.AxisMinValue)
            return AxisX.AxisMinValue;

        double maxValue = AxisX.AxisMinValue + AxisX.AxisSize;

        return value > maxValue ? maxValue : value;
    }

    #endregion
}