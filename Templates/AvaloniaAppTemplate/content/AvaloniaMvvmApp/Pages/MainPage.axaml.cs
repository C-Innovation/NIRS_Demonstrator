using AvaloniaMvvmApp.ViewModels;

namespace AvaloniaMvvmApp;

/// <summary>
/// Главная страница приложения.
/// <para>
/// Вся логика — в <see cref="MainPageViewModel"/>; здесь только разметка.
/// Заводя свою страницу, скопируйте эту пару файлов и зарегистрируйте
/// страницу в <c>PageRegistry.RegisterDefaults</c>.
/// </para>
/// </summary>
public partial class MainPage : BasePage<MainPageViewModel>
{
    public MainPage() : this(null)
    {
    }

    public MainPage(MainPageViewModel? specificViewModel) : base(specificViewModel)
    {
        InitializeComponent();
    }
}
