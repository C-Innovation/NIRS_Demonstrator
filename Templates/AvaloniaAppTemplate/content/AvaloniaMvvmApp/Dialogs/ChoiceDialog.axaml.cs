using Avalonia;
using Avalonia.Interactivity;
using AvaloniaDialogs.Views;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.Dialogs;

/// <summary>
/// Запрос с двумя или тремя текстовыми кнопками.
/// Нейтральная кнопка появляется, только если задан её текст.
/// </summary>
public partial class ChoiceDialog : BaseDialog<MessageBoxResult>
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ChoiceDialog, string>(nameof(Message), string.Empty);

    /// <summary>Текст вопроса.</summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly StyledProperty<string> PositiveTextProperty =
        AvaloniaProperty.Register<ChoiceDialog, string>(nameof(PositiveText), "OK");

    /// <summary>Надпись утвердительной кнопки. Возвращает <see cref="MessageBoxResult.Yes"/>.</summary>
    public string PositiveText
    {
        get => GetValue(PositiveTextProperty);
        set => SetValue(PositiveTextProperty, value);
    }

    public static readonly StyledProperty<string> NegativeTextProperty =
        AvaloniaProperty.Register<ChoiceDialog, string>(nameof(NegativeText), "Cancel");

    /// <summary>Надпись отрицательной кнопки. Возвращает <see cref="MessageBoxResult.No"/>.</summary>
    public string NegativeText
    {
        get => GetValue(NegativeTextProperty);
        set => SetValue(NegativeTextProperty, value);
    }

    public static readonly StyledProperty<string?> NeutralTextProperty =
        AvaloniaProperty.Register<ChoiceDialog, string?>(nameof(NeutralText));

    /// <summary>
    /// Надпись нейтральной кнопки. <c>null</c> или пусто — кнопка скрыта.
    /// Возвращает <see cref="MessageBoxResult.Cancel"/>.
    /// </summary>
    public string? NeutralText
    {
        get => GetValue(NeutralTextProperty);
        set => SetValue(NeutralTextProperty, value);
    }

    public ChoiceDialog()
    {
        InitializeComponent();

        MessageTextBlock.Text = Message;
        PositiveButton.Content = PositiveText;
        NegativeButton.Content = NegativeText;

        PositiveButton.Click += PositiveButton_Click;
        NegativeButton.Click += NegativeButton_Click;
        NeutralButton.Click += NeutralButton_Click;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == MessageProperty && MessageTextBlock != null)
        {
            MessageTextBlock.Text = change.GetNewValue<string>();
        }
        else if (change.Property == PositiveTextProperty && PositiveButton != null)
        {
            PositiveButton.Content = change.GetNewValue<string>();
        }
        else if (change.Property == NegativeTextProperty && NegativeButton != null)
        {
            NegativeButton.Content = change.GetNewValue<string>();
        }
        else if (change.Property == NeutralTextProperty && NeutralButton != null)
        {
            string? text = change.GetNewValue<string?>();
            NeutralButton.Content = text;
            NeutralButton.IsVisible = !string.IsNullOrEmpty(text);
        }
    }

    private void PositiveButton_Click(object? sender, RoutedEventArgs e) => Close(MessageBoxResult.Yes);

    private void NegativeButton_Click(object? sender, RoutedEventArgs e) => Close(MessageBoxResult.No);

    private void NeutralButton_Click(object? sender, RoutedEventArgs e) => Close(MessageBoxResult.Cancel);
}
