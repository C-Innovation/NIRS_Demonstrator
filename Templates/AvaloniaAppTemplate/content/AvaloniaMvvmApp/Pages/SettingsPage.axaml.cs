using AvaloniaMvvmApp.ViewModels;

namespace AvaloniaMvvmApp;

/// <summary>
/// Страница настроек: выбор цветовой темы и языка интерфейса.
/// </summary>
public partial class SettingsPage : BasePage<SettingsPageViewModel>
{
    public SettingsPage() : this(null)
    {
    }

    public SettingsPage(SettingsPageViewModel? specificViewModel) : base(specificViewModel)
    {
        InitializeComponent();
    }
}
