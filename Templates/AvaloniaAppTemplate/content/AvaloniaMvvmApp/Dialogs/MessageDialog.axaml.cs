using Avalonia;
using Avalonia.Interactivity;
using AvaloniaDialogs.Views;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.Dialogs;

/// <summary>
/// Сообщение с единственной кнопкой подтверждения.
/// </summary>
public partial class MessageDialog : BaseDialog<MessageBoxResult>
{
    /// <summary>
    /// Текст сообщения.
    /// </summary>
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<MessageDialog, string>(nameof(Message), string.Empty);

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Надпись на кнопке.
    /// </summary>
    public static readonly StyledProperty<string> ButtonTextProperty =
        AvaloniaProperty.Register<MessageDialog, string>(nameof(ButtonText), "OK");

    public string ButtonText
    {
        get => GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public MessageDialog()
    {
        InitializeComponent();

        MessageTextBlock.Text = Message;
        OkButton.Content = ButtonText;
        OkButton.Click += OkButton_Click;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Свойство может измениться до того, как InitializeComponent присвоит
        // поля именованных элементов, поэтому обращение защищённое.
        if (change.Property == MessageProperty && MessageTextBlock != null)
            MessageTextBlock.Text = change.GetNewValue<string>();
        else if (change.Property == ButtonTextProperty && OkButton != null)
            OkButton.Content = change.GetNewValue<string>();
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Ok);
    }
}
