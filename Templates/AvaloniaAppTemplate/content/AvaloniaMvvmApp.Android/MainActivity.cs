using Android.App;
using Android.Content.PM;
using Avalonia.Android;

// Класс намеренно объявлен в глобальном пространстве имён.
// Внутри пространства имён вида AvaloniaMvvmApp.Android имя Android в директивах
// using разрешалось бы неоднозначно (Android.App против AvaloniaMvvmApp.Android).

/// <summary>
/// Активность-носитель приложения на Android.
/// <para>
/// В Avalonia 12 активность больше не параметризуется типом приложения и не
/// настраивает <c>AppBuilder</c> — этим занимается <see cref="MainApplication"/>.
/// Представление создаётся фабрикой
/// <c>IActivityApplicationLifetime.MainViewFactory</c> в <c>App</c>, поэтому
/// активность можно пересоздавать без повторной настройки Avalonia.
/// </para>
/// </summary>
[Activity(
    Label = "AvaloniaMvvmApp",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/Icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}
