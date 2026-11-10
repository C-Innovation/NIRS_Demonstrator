using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaMvvmApp.Core;
using AvaloniaMvvmApp.Navigation;

namespace AvaloniaMvvmApp;

/// <summary>
/// Контейнер страниц с анимацией перехода на базе <see cref="Carousel"/>.
/// <para>
/// О конкретных страницах не знает ничего: представление создаётся через
/// <see cref="PageRegistry"/>, а ось и направление анимации берутся из описания
/// страницы. Добавление страницы не требует правок здесь.
/// </para>
/// </summary>
public partial class PageHost : UserControl
{
    #region Свойства

    /// <summary>
    /// Отображаемая страница. Привязывается к <see cref="ApplicationViewModel.CurrentPage"/>.
    /// </summary>
    public static readonly StyledProperty<ApplicationPage> CurrentPageProperty =
        AvaloniaProperty.Register<PageHost, ApplicationPage>(nameof(CurrentPage));

    public ApplicationPage CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>
    /// Модель представления для отображаемой страницы.
    /// Привязывается к <see cref="ApplicationViewModel.CurrentPageViewModel"/>.
    /// </summary>
    public static readonly StyledProperty<ViewModelBase?> CurrentPageViewModelProperty =
        AvaloniaProperty.Register<PageHost, ViewModelBase?>(nameof(CurrentPageViewModel));

    public ViewModelBase? CurrentPageViewModel
    {
        get => GetValue(CurrentPageViewModelProperty);
        set => SetValue(CurrentPageViewModelProperty, value);
    }

    /// <summary>
    /// Длительность анимации перехода.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> TransitionDurationProperty =
        AvaloniaProperty.Register<PageHost, TimeSpan>(nameof(TransitionDuration), TimeSpan.FromMilliseconds(250));

    public TimeSpan TransitionDuration
    {
        get => GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    #endregion

    #region Поля

    /// <summary>
    /// Переходы выполняются по одному: иначе быстрые нажатия наложатся
    /// и карусель останется с лишними элементами.
    /// </summary>
    private readonly SemaphoreSlim _transitionLock = new SemaphoreSlim(1, 1);

    private ContentControl? _currentItem;
    private ApplicationPage _displayedPage;

    #endregion

    public PageHost()
    {
        InitializeComponent();

        // В конструкторе режима конструктора контейнер зависимостей не собран,
        // поэтому страницу не создаём — предварительный просмотр остаётся пустым.
        if (Design.IsDesignMode)
            return;

        _displayedPage = IoC.Application.CurrentPage;
        _currentItem = new ContentControl
        {
            Content = PageRegistry.CreatePage(_displayedPage, IoC.Application.CurrentPageViewModel),
        };

        PagesCarousel.Items.Add(_currentItem);
        PagesCarousel.SelectedIndex = 0;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != CurrentPageProperty || Design.IsDesignMode)
            return;

        var targetPage = change.GetNewValue<ApplicationPage>();

        // Та же страница — анимация не нужна, достаточно обновить модель представления.
        if (_currentItem?.Content is BasePage page
            && PageRegistry.GetApplicationPage(page) == targetPage)
        {
            page.ViewModelObject = CurrentPageViewModel;
            return;
        }

        _ = NavigateAsync(targetPage, CurrentPageViewModel);
    }

    /// <summary>
    /// Подкладывает новую страницу в карусель, проигрывает переход
    /// и убирает прежнюю страницу.
    /// </summary>
    private async Task NavigateAsync(ApplicationPage targetPage, ViewModelBase? viewModel)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var previousPage = _displayedPage;
            var (axis, forward) = PageRegistry.GetTransition(previousPage, targetPage);

            PagesCarousel.PageTransition = new PageSlide(
                TransitionDuration,
                axis == PageTransitionAxis.Vertical
                    ? PageSlide.SlideAxis.Vertical
                    : PageSlide.SlideAxis.Horizontal)
            {
                SlideInEasing = new QuadraticEaseIn(),
                SlideOutEasing = new QuadraticEaseOut(),
            };

            var outgoingItem = _currentItem;
            var incomingItem = new ContentControl
            {
                Content = PageRegistry.CreatePage(targetPage, viewModel),
            };

            if (forward)
            {
                // Новая страница встаёт справа (снизу) — карусель едет вперёд.
                PagesCarousel.Items.Add(incomingItem);
                PagesCarousel.SelectedIndex = PagesCarousel.Items.Count - 1;
            }
            else
            {
                // Новая страница встаёт слева (сверху): после вставки выделение
                // смещается на прежнюю страницу, и Previous уезжает назад к новой.
                PagesCarousel.Items.Insert(0, incomingItem);
                PagesCarousel.SelectedIndex = 1;
                PagesCarousel.Previous();
            }

            _currentItem = incomingItem;
            _displayedPage = targetPage;

            // Убирать прежнюю страницу можно только после того, как анимация
            // доиграла, иначе она исчезнет рывком.
            await Task.Delay(TransitionDuration);

            if (outgoingItem != null)
            {
                PagesCarousel.Items.Remove(outgoingItem);
                outgoingItem.Content = null;
            }
        }
        finally
        {
            _transitionLock.Release();
        }
    }
}
