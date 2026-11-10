using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using AvaloniaMvvmApp.Core;
using AvaloniaMvvmApp.Navigation;
using AvaloniaMvvmApp.ViewModels;
using AvaloniaMvvmApp.Views;

namespace AvaloniaMvvmApp;

public partial class App : Application
{
    /// <summary>
    /// Имя приложения. Задаёт каталоги настроек, журнала и отчётов.
    /// </summary>
    public const string APP_NAME = "AvaloniaMvvmApp";

    /// <summary>
    /// Имя организации. Добавляется в путь к каталогам данных.
    /// Пустая строка — каталоги создаются сразу под именем приложения.
    /// </summary>
    public const string COMPANY_NAME = "COMPANY_NAME_PLACEHOLDER";

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Порядок важен: темы и языки регистрируются до чтения настроек,
        // иначе сохранённый выбор не к чему применить.
        var config = AppConfig.GetInstance();
        RegisterThemesAndLanguages(config);
        config.Initialize(APP_NAME, COMPANY_NAME);

        IoC.Setup(ConfigureServices);
        PageRegistry.RegisterDefaults();

        IoC.Logger.Log("Приложение запускается…", LogLevel.Success);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };

            config.SetApplicationLifetime(desktop);
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            // Android в Avalonia 12: представление создаётся по требованию,
            // потому что активность может быть пересоздана (поворот экрана,
            // возврат из фона). Проверка идёт раньше ISingleViewApplicationLifetime.
            activity.MainViewFactory = () => CreateMainView(config);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // iOS, браузер, встроенный Linux.
            singleView.MainView = CreateMainView(config);
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Создаёт главное представление однооконной платформы и подключает его
    /// к освобождению ресурсов.
    /// </summary>
    private static MainView CreateMainView(AppConfig config)
    {
        var view = new MainView
        {
            DataContext = new MainViewModel(),
        };

        config.AttachToMainView(view);
        return view;
    }

    /// <summary>
    /// Регистрация служб приложения.
    /// <para>
    /// Базовые службы (файлы, задачи, навигация) уже зарегистрированы в
    /// <see cref="IoC.Setup"/>. Здесь добавляются те, что зависят от платформы
    /// или от путей, вычисленных <see cref="AppConfig"/>.
    /// </para>
    /// </summary>
    private static void ConfigureServices(IServiceCollection services)
    {
        var config = AppConfig.GetInstance();

        services.AddSingleton<ILogFactory>(_ => new BaseLogFactory(new ILogger[]
        {
            new FileLogger(config.GetLogFilePath()),
        }));

        services.AddSingleton<IUIManager, UIManager>();

        // Модели представления страниц регистрировать не обязательно:
        // IoC.Get создаст их через контейнер, внедрив зарегистрированные
        // зависимости. Регистрация нужна, если модель должна быть одиночкой
        // либо требует особого времени жизни.
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
    }

    /// <summary>
    /// Цветовые темы и языки интерфейса, поставляемые шаблоном.
    /// <para>
    /// Добавляя тему или язык: положите словарь в <c>Styles/Themes</c> либо
    /// <c>Styles/Languages</c> и допишите одну строку регистрации — страница
    /// настроек подхватит вариант сама.
    /// </para>
    /// </summary>
    private static void RegisterThemesAndLanguages(AppConfig config)
    {
        config.RegisterTheme(AppThemes.Light, "ThemeLightText",
            "avares://AvaloniaMvvmApp/Styles/Themes/LightTheme.axaml");

        config.RegisterTheme(AppThemes.Dark, "ThemeDarkText",
            "avares://AvaloniaMvvmApp/Styles/Themes/DarkTheme.axaml");

        config.RegisterLanguage(AppLanguages.English, "LanguageEnglishText",
            "avares://AvaloniaMvvmApp/Styles/Languages/en-US.axaml");

        config.RegisterLanguage(AppLanguages.Russian, "LanguageRussianText",
            "avares://AvaloniaMvvmApp/Styles/Languages/ru-RU.axaml");
    }
}
