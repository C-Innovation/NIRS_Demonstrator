using Avalonia;
using Avalonia.iOS;
using Foundation;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.FontAwesome;

namespace AvaloniaMvvmApp.iOS;

/// <summary>
/// Точка входа приложения на iOS.
/// <para>
/// Общая разметка берётся из проекта AvaloniaMvvmApp: на этой платформе работает
/// ветка <c>ISingleViewApplicationLifetime</c> в <c>App</c>.
/// </para>
/// </summary>
[Register("AppDelegate")]
#pragma warning disable CA1711 // Суффикс Delegate требуется средой iOS
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711
{
    /// <inheritdoc />
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        // Поставщик иконок регистрируется до первой отрисовки:
        // к нему обращаются стили кнопок.
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        return base.CustomizeAppBuilder(builder)
                   .WithInterFont();
    }
}
