using Avalonia.Controls;

namespace AvaloniaMvvmApp.Views;

/// <summary>
/// Главное окно настольного приложения.
/// Содержит <see cref="MainView"/>, чтобы разметка была общей
/// с однооконными платформами.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
