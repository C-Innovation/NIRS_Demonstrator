using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaMvvmApp.Views;

/// <summary>
/// Корневое представление приложения. На настольных платформах лежит внутри
/// <see cref="MainWindow"/>, на однооконных используется напрямую.
/// </summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        SizeChanged += MainView_SizeChanged;
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        AppConfig.GetInstance().NotifyMainViewLoaded(this);
    }

    private void MainView_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Размер корневого представления нужен элементам, которые верстаются
        // в коде; подписываться на окно из каждого такого элемента неудобно.
        AppConfig.GetInstance().NotifyMainViewSizeChanged(this, e);
    }
}
