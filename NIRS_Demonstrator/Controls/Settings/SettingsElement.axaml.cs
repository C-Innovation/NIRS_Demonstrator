using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace NIRS_Demonstrator;

public partial class SettingsElement : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string> HeaderProperty =
       AvaloniaProperty.Register<SettingsElement, string>(nameof(Header), defaultValue: string.Empty);

    public string Header
    {
        get { return (string)GetValue(HeaderProperty); }
        set { SetValue(HeaderProperty, value); }
    }

    public static readonly StyledProperty<string> IconProperty =
       AvaloniaProperty.Register<SettingsElement, string>(nameof(Icon), defaultValue: string.Empty);

    public string Icon
    {
        get { return (string)GetValue(IconProperty); }
        set { SetValue(IconProperty, value); }
    }

    public static readonly StyledProperty<bool> CheckStateProperty =
   AvaloniaProperty.Register<SettingsElement, bool>(nameof(CheckState), defaultValue: false);

    public bool CheckState
    {
        get { return (bool)GetValue(CheckStateProperty); }
        set { SetValue(CheckStateProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == HeaderProperty)
        {
            HeaderText.Text = (string)change.NewValue;
        }
        else if (change.Property == IconProperty)
        {
            IconControl.Source = new Bitmap(AssetLoader.Open(new Uri("avares://NIRS_Demonstrator/Assets/" + (string)change.NewValue))); //
        }
        else if (change.Property == CheckStateProperty)
        {
            CheckBoxButton.Content = (bool)change.NewValue ? "fa-regular fa-square-check" : "fa-regular fa-square";
        }

        base.OnPropertyChanged(change);

    }

    #endregion

    #region Public Properties

    public int ID { get; set; }

    #endregion

    #region Public Events

    public event EventHandler<bool> CheckStateChangedChanged;

    #endregion

    public SettingsElement()
    {
        InitializeComponent();
    }

    #region Private Callbacks

    private void CheckBoxButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.CheckState == true)
            return;
        this.CheckState ^= true;
        CheckStateChangedChanged?.Invoke(this, CheckState);
    }

    #endregion

    #region Private Methods

    #endregion
}