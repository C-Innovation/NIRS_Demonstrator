using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using NIRS_Demonstrator.Core;
using NIRS_Demonstrator.ViewModels;

namespace NIRS_Demonstrator;

public partial class SettingsPage : BasePage<SettingsPageViewModel>
{
    private AppTheme AppTheme;
    private AppLanguage AppLanguage;
    public string ReportsDirPath;
    public string DeviceMac;
    public string DeviceName;
    public bool ConnectOnStart;
    public int UartSpeed;

    public SettingsPage() : base()
    {
        InitializeComponent();
        AppTheme = AppConfig.GetInstance().AppTheme;
        AppLanguage = AppConfig.GetInstance().AppLanguage;
        ReportsDirPath = AppConfig.GetInstance().ReportsDirPath;
        DeviceMac = AppConfig.GetInstance().DeviceMac;
        DeviceName = AppConfig.GetInstance().DeviceName;
        ConnectOnStart = AppConfig.GetInstance().ConnectOnStart;
        
        UartSpeed = AppConfig.GetInstance().UartSpeed;
        Loaded += SettingsPage_Loaded;
    }


    /// <summary>
    /// Extended Constructor
    /// </summary>
    /// <param name="specificViewModel"></param>
    public SettingsPage(SettingsPageViewModel specificViewModel = null) : base(specificViewModel)
    {
        InitializeComponent();
        AppTheme = AppConfig.GetInstance().AppTheme;
        AppLanguage = AppConfig.GetInstance().AppLanguage;
        ReportsDirPath = AppConfig.GetInstance().ReportsDirPath;
;
        ConnectOnStart = AppConfig.GetInstance().ConnectOnStart;
        
        UartSpeed = AppConfig.GetInstance().UartSpeed;
        Loaded += SettingsPage_Loaded;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        InterfaceLanguageSelection.SetSelection((int)AppConfig.GetInstance().AppLanguage);
        ColorThemeSelection.SetSelection((int)AppConfig.GetInstance().AppTheme);

    }
       

    private void InterfaceLanguageSelection_SettingsSelectionChanged(object sender, int e)
    {
        AppConfig.GetInstance().ChangeLang((AppLanguage)e);
    }


    private void ColorThemeSelection_SettingsSelectionChanged(object sender, int e)
    {
        AppConfig.GetInstance().ChangeTheme((AppTheme)e);
    }

    private void ConnectOnStartupSelector_CheckStateChanged(object? sender, bool e)
    {
        ConnectOnStart = e;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        AppConfig.GetInstance().ChangeLang(AppLanguage);
        AppConfig.GetInstance().ChangeTheme(AppTheme);
        IoC.Application.GoToPage(ApplicationPage.Main);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        
        AppConfig.GetInstance().SaveAppConfig();
        IoC.Application.GoToPage(ApplicationPage.Main);
    }
}