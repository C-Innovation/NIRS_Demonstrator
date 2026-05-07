using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.ViewModels;
using NIRS_Demonstrator.Views;
using System;
using System.Diagnostics;
using System.IO;

namespace NIRS_Demonstrator;

public partial class App : Application
{
    public const string APP_CINNOVATION_ROOT = "C-Innovation";
    public const string APP_SYS_SECTION = "Local";
    public const string APP_CONFIG_DIR_NAME = ".sys";
    public const string APP_DICTIONARIES_DIR_NAME = ".dictionaries";
    public const string APP_AI_FOLDER_NAME = "AI";
    public const string APP_REPORTS_FOLDER_NAME = "Reports";
    public const string APP_NAME = "NIRS_Demonstrator";

    public static IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// Вызывается из платформенного кода для регистрации общих сервисов.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Регистрируем ViewModels и другие общие сервисы
        services.AddTransient<MainViewModel>();
        // ... другие общие регистрации
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ServiceProvider == null)
#if RELEASE
            throw new InvalidOperationException(
                    "ServiceProvider not configured. Call App.ConfigureServices and set App.ServiceProvider from platform code.");
#else
            Debugger.Break();
#endif

        if (OperatingSystem.IsWindows())
        {

            string path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)).FullName;
            AppConfig.GetInstance().SettingsDirectoryPath = (Path.Combine(path, APP_SYS_SECTION, APP_CINNOVATION_ROOT, APP_NAME, APP_CONFIG_DIR_NAME));
            AppConfig.GetInstance().ReportsDirectoryPath = (Path.Combine(path, APP_SYS_SECTION, APP_CINNOVATION_ROOT, APP_NAME, APP_REPORTS_FOLDER_NAME));
            AppConfig.GetInstance().DictionariesDirectoryPath = (Path.Combine(path, APP_SYS_SECTION, APP_CINNOVATION_ROOT, APP_NAME, APP_DICTIONARIES_DIR_NAME));
            AppConfig.GetInstance().AiDirectoryPath = (Path.Combine(AppDomain.CurrentDomain.BaseDirectory, APP_AI_FOLDER_NAME));
            //string path2 = path + "\\" + APP_SYS_SECTION + "\\" + APP_MEDICOM_ROOT + "\\" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + "\\" + APP_CONFIG_DIR_NAME;
        }
        AppConfig.GetInstance().InitializeAppConfig();

        IoC.Setup();

        string logPath = Path.Combine(AppConfig.GetInstance().SettingsDirectoryPath, "log.txt");
        //string assemblyLocation = Assembly.GetExecutingAssembly().Location;
        //string logPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "log.txt");
        //
        IoC.Kernel.Bind<ILogFactory>().ToConstant(new BaseLogFactory(new[]
        {
                // TODO: Add app settings to configure log location
                //
                new FileLogger(logPath),
            }));

        //
        IoC.Kernel.Bind<IFileManager>().ToConstant(new FileManager());

        //
        IoC.Kernel.Bind<ITaskManager>().ToConstant(new TaskManager());

        //
        IoC.Kernel.Bind<IUIManager>().ToConstant(new UIManager());

        //
        IoC.Logger.Log("Application starting...", LogLevel.Success);


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            AppConfig.GetInstance().SetApplicationLifetime(desktop);
        }
        
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
            AppConfig.GetInstance().SetApplicationLifetime(singleViewPlatform);
        }

        base.OnFrameworkInitializationCompleted();
    }
}