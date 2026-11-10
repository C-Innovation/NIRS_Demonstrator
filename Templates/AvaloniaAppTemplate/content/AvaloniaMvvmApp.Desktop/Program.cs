using Avalonia;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.FontAwesome;
using System;

namespace AvaloniaMvvmApp.Desktop;

/// <summary>
/// Точка входа настольного приложения.
/// </summary>
internal sealed class Program
{
    /// <summary>
    /// До вызова <see cref="BuildAvaloniaApp"/> нельзя обращаться к Avalonia
    /// и к коду, которому нужен контекст синхронизации: он ещё не создан.
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Конфигурация Avalonia.
    /// <para>
    /// Имя метода менять нельзя. Конструктор предварительного просмотра
    /// разметки в Visual Studio и Rider ищет в точке входа открытый статический
    /// метод с именем <c>BuildAvaloniaApp</c>, возвращающий <see cref="AppBuilder"/>.
    /// При другом имени предпросмотр падает с
    /// <c>InvalidOperationException: Unable to create AppBuilder from type</c>.
    /// </para>
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
    {
        // Поставщик иконок регистрируется до запуска приложения:
        // стили кнопок обращаются к нему при первой отрисовке.
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<App>()
                         .UsePlatformDetect()
#if DEBUG
                         .WithDeveloperTools()
#endif
                         .WithInterFont()
                         .LogToTrace();
    }
}
