using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NIRS_Demonstrator;

public partial class MainMenuItemControl : UserControl
{
    #region Dependency (Styled or Direct) Properties

    public static readonly StyledProperty<ICommand> CommandProperty =
    AvaloniaProperty.Register<MainMenuItemControl, ICommand>(nameof(Command), defaultValue: null);

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly StyledProperty<object> CommandParameterProperty =
    AvaloniaProperty.Register<MainMenuItemControl, object>(nameof(CommandParameter), defaultValue: null);

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly StyledProperty<string> IconProperty =
    AvaloniaProperty.Register<MainMenuItemControl, string>(nameof(Icon), defaultValue: "fa-solid fa-circle");

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    // Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MainMenuItemControl, string>(nameof(Header), defaultValue: string.Empty);

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    #endregion


    private BrushAnimation _PointerAnimation;
    private const double E_WIDTH = 170;
    private const double E_HEIGHT = 200;
    private const double E_ICON = 96;
    private const double E_HEADER = 24;
    private const double E_MARGIN = 5;



    #region Public Events



    public event EventHandler Click;

    #endregion

    public MainMenuItemControl()
    {
        InitializeComponent();
        //DataContext = new MainMenuItemControlViewModel(this);
        _PointerAnimation = new BrushAnimation();
        //SetupAnimation();
        Tapped += MainMenuItemControl_Tapped;

        PointerExited += MainMenuItemControl_PointerExited;
        PointerEntered += MainMenuItemControl_PointerEntered;
        SizeChanged += MainMenuItemControl_SizeChanged;
    }

    private void MainMenuItemControl_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (e.HeightChanged)
        {
            double coefDelta = e.NewSize.Height / E_HEIGHT;
            this.Width = E_WIDTH * coefDelta;
            this.ButtonIcon.FontSize = E_ICON * coefDelta;
            this.HeaderTextBlock.FontSize = E_HEADER * coefDelta;
            this.Margin = new Thickness(E_MARGIN * coefDelta);
        }
        else if (e.WidthChanged) 
        {
            double coefDelta = e.NewSize.Width / E_WIDTH;
            this.Height = E_HEIGHT * coefDelta;
            this.ButtonIcon.FontSize = E_ICON * coefDelta;
            this.HeaderTextBlock.FontSize = E_HEADER * coefDelta;
            this.Margin = new Thickness(E_MARGIN * coefDelta);
        }
    }

    private void MainMenuItemControl_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        //Debug.WriteLine("MainMenuItemControl_PointerEntered");
        //_BorderAnimation.PlaybackDirection = PlaybackDirection.Normal;
        //_BorderAnimation.RunAsync(MainBorder);
        //Brush brush;
        this.TryFindResource("Theme_GreekBlueBrush", ActualThemeVariant, out var brush);
        _PointerAnimation.RunAsync(MainBorder.BorderBrush, (IBrush?)brush, Border.BorderBrushProperty, MainBorder, durationMs: 200);
        _PointerAnimation.RunAsync(ButtonIcon.Foreground, (IBrush?)brush, Projektanker.Icons.Avalonia.Icon.ForegroundProperty, ButtonIcon, durationMs: 200);
        _PointerAnimation.RunAsync(HeaderTextBlock.Foreground, (IBrush?)brush, TextBlock.ForegroundProperty, HeaderTextBlock, durationMs: 200);
        
        
    }

    private void MainMenuItemControl_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        //Debug.WriteLine("MainMenuItemControl_PointerExited");
        _PointerAnimation.RunAsync(MainBorder.BorderBrush, AppConfig.GetInstance().ForegroundMainBrush, Border.BorderBrushProperty, MainBorder, durationMs: 200);
        _PointerAnimation.RunAsync(ButtonIcon.Foreground, AppConfig.GetInstance().ForegroundMainBrush, Projektanker.Icons.Avalonia.Icon.ForegroundProperty, ButtonIcon, durationMs: 200);
        _PointerAnimation.RunAsync(HeaderTextBlock.Foreground, AppConfig.GetInstance().ForegroundMainBrush, TextBlock.ForegroundProperty, HeaderTextBlock, durationMs: 200);
        //_BorderAnimation.PlaybackDirection = PlaybackDirection.Reverse;
        //_BorderAnimation.RunAsync(MainBorder);

    }



    private async void MainMenuItemControl_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            this.TryFindResource("Theme_GreekBlueBrush", ActualThemeVariant, out var brush);
            MainBorder.BorderBrush = (IBrush?)brush;
            ButtonIcon.Foreground = (IBrush?)brush;
            HeaderTextBlock.Foreground = (IBrush?)brush;
            await Task.Delay(100);
            MainBorder.BorderBrush = AppConfig.GetInstance().ForegroundMainBrush;
            ButtonIcon.Foreground = AppConfig.GetInstance().ForegroundMainBrush;
            HeaderTextBlock.Foreground = AppConfig.GetInstance().ForegroundMainBrush;
        }
            
        Click?.Invoke(this, EventArgs.Empty);
        Command?.Execute(CommandParameter);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if(change == null) return;

        if (change.Property == CommandProperty)
        {
            //Debugger.Break();
        }
        else if (change.Property == IconProperty)
        {
            ButtonIcon.Value = (string)change.NewValue;
        }
        else if(change.Property == HeaderProperty)
        {
            HeaderTextBlock.Text = (string)change.NewValue;
        }

        base.OnPropertyChanged(change);

    }

   
   
}