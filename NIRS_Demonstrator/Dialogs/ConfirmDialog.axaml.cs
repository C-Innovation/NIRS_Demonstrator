using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaDialogs.Views;
using NIRS_Demonstrator.Core;

namespace NIRS_Demonstrator;

public partial class ConfirmDialog : BaseDialog<MessageBoxResult>
{
    #region Styled Properties

    public static readonly StyledProperty<string> MessageProperty =
       AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message), defaultValue: string.Empty);

    public string Message
    {
        get { return (string)GetValue(MessageProperty); }
        set { SetValue(MessageProperty, value); }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change == null) return;

        if (change.Property == MessageProperty)
        {
            MessageTextBlock.Text = (string)change.NewValue;
        }


        base.OnPropertyChanged(change);

    }

    #endregion

    public ConfirmDialog()
    {
        InitializeComponent();
        YesButton.Click += YesButton_Click;
        NoButton.Click += NoButton_Click;
    }

    private void NoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MessageBoxResult.No);
    }

    private void YesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MessageBoxResult.Yes);
    }
}