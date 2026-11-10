using Avalonia;
using Avalonia.Browser;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.FontAwesome;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Browser;

/// <summary>
/// Точка входа приложения в браузере (WebAssembly).
/// <para>
/// Общая разметка берётся из проекта AvaloniaMvvmApp: используется ветка
/// <c>ISingleViewApplicationLifetime</c> в <c>App</c>.
/// </para>
/// </summary>
internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        return BuildAvaloniaApp()
               .WithInterFont()
#if DEBUG
               .WithDeveloperTools()
#endif
               .StartBrowserAppAsync("out");
    }

    /// <summary>
    /// Имя метода менять нельзя — см. пояснение в точке входа настольного
    /// приложения.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<global::AvaloniaMvvmApp.App>();
}
