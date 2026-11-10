using Avalonia;
using Avalonia.Interactivity;
using AvaloniaDialogs.Views;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.Dialogs;

/// <summary>
/// Запрос «да/нет» с кнопками-иконками.
/// </summary>
public partial class ConfirmDialog : BaseDialog<MessageBoxResult>
{
    /// <summary>
    /// Текст вопроса.
    /// </summary>
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmDialog, string>(nameof(Message), string.Empty);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ConfirmDialog()
    {
        InitializeComponent();

        MessageTextBlock.Text = Message;
        YesButton.Click += YesButton_Click;
        NoButton.Click += NoButton_Click;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MessageProperty && MessageTextBlock != null)
            MessageTextBlock.Text = change.GetNewValue<string>();
    }

    private void YesButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Yes);
    }

    private void NoButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.No);
    }
}
