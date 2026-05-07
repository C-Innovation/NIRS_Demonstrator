using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace NIRS_Demonstrator;

public partial class CheckButton : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsCheckedProperty =
       AvaloniaProperty.Register<CheckButton, bool>(nameof(IsChecked), defaultValue: false);

    public bool IsChecked
    {
        get { return (bool)GetValue(IsCheckedProperty); }
        set { SetValue(IsCheckedProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == IsCheckedProperty)
        {
            ActionButton.Content = ((bool)change?.NewValue ? "fa-regular fa-square-check" : "fa-regular fa-square");
        }

        base.OnPropertyChanged(change);
    }

    #endregion

    #region Public Events

    public event EventHandler<bool> CheckStateChanged;

    #endregion

    public CheckButton()
    {
        InitializeComponent();
    }

    #region Private Callbacks

    private void ActionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        IsChecked ^= true;
        CheckStateChanged?.Invoke(this, IsChecked);
    }

    #endregion
}