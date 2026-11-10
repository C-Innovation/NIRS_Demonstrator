using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using AvaloniaMvvmApp;
using Optris.Icons.Avalonia;
using Optris.Icons.Avalonia.FontAwesome;
using System;

// Глобальное пространство имён — по той же причине, что и у MainActivity:
// иначе имена Android и App разрешались бы неоднозначно.

/// <summary>
/// Настройка Avalonia на Android.
/// <para>
/// В Avalonia 12 <c>AppBuilder</c> собирается на уровне приложения, а не
/// активности: приложение живёт дольше любой активности, поэтому настройка
/// выполняется один раз. Общая разметка берётся из проекта AvaloniaMvvmApp —
/// на этой платформе работает ветка <c>IActivityApplicationLifetime</c>
/// в <c>App</c>, то есть показывается <c>MainView</c> без окна.
/// </para>
/// </summary>
[Application]
public class MainApplication : AvaloniaAndroidApplication<App>
{
    /// <summary>
    /// Конструктор вызывается средой выполнения Android при создании процесса.
    /// Своей логики здесь быть не должно: Avalonia ещё не настроена.
    /// </summary>
    protected MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

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
