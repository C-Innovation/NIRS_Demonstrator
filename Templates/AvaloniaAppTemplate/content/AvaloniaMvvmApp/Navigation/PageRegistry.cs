using System;
using System.Collections.Generic;
using AvaloniaMvvmApp.Core;
using AvaloniaMvvmApp.ViewModels;

namespace AvaloniaMvvmApp.Navigation
{
    /// <summary>
    /// Ось, по которой едет анимация перехода между страницами.
    /// </summary>
    public enum PageTransitionAxis
    {
        /// <summary>Горизонтальный сдвиг. Поведение по умолчанию.</summary>
        Horizontal = 0,

        /// <summary>Вертикальный сдвиг. Подходит для служебных страниц вроде настроек.</summary>
        Vertical = 1,
    }

    /// <summary>
    /// Описание страницы: как её создать и как анимировать переход.
    /// </summary>
    /// <param name="Factory">
    /// Фабрика страницы. Аргумент — модель представления либо <c>null</c>,
    /// тогда страница создаёт модель сама.
    /// </param>
    /// <param name="Axis">Ось анимации перехода.</param>
    /// <param name="Order">
    /// Положение страницы на оси. Знак разности порядков задаёт направление
    /// анимации: переход к большему порядку едет «вперёд», к меньшему — «назад».
    /// Главная страница — 0, служебные страницы вроде настроек — отрицательные,
    /// рабочие — положительные.
    /// </param>
    public sealed record PageDescriptor(
        Func<ViewModelBase?, BasePage> Factory,
        PageTransitionAxis Axis,
        int Order);

    /// <summary>
    /// Реестр страниц приложения.
    /// <para>
    /// Заменяет switch-блоки «страница → представление» и «представление → страница»:
    /// добавление страницы сводится к одной регистрации, а <c>PageHost</c> берёт
    /// параметры анимации из описания, не зная о конкретных страницах.
    /// </para>
    /// </summary>
    public static class PageRegistry
    {
        private static readonly Dictionary<ApplicationPage, PageDescriptor> _descriptors
            = new Dictionary<ApplicationPage, PageDescriptor>();

        private static readonly Dictionary<Type, ApplicationPage> _pageTypes
            = new Dictionary<Type, ApplicationPage>();

        /// <summary>
        /// Регистрирует страницу.
        /// </summary>
        /// <typeparam name="TPage">Тип представления страницы.</typeparam>
        /// <param name="page">Значение перечисления страниц.</param>
        /// <param name="factory">Фабрика страницы.</param>
        /// <param name="axis">Ось анимации перехода.</param>
        /// <param name="order">Положение страницы на оси.</param>
        public static void Register<TPage>(ApplicationPage page,
                                           Func<ViewModelBase?, TPage> factory,
                                           PageTransitionAxis axis = PageTransitionAxis.Horizontal,
                                           int order = 1)
            where TPage : BasePage
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _descriptors[page] = new PageDescriptor(vm => factory(vm), axis, order);
            _pageTypes[typeof(TPage)] = page;
        }

        /// <summary>
        /// Регистрации страниц, поставляемых шаблоном.
        /// Вызывается один раз при запуске приложения.
        /// </summary>
        public static void RegisterDefaults()
        {
            Register(ApplicationPage.Main,
                     vm => new MainPage(vm as MainPageViewModel),
                     PageTransitionAxis.Horizontal,
                     order: 0);

            // Настройки уезжают вниз: отрицательный порядок и вертикальная ось.
            Register(ApplicationPage.Settings,
                     vm => new SettingsPage(vm as SettingsPageViewModel),
                     PageTransitionAxis.Vertical,
                     order: -1);
        }

        /// <summary>
        /// Создаёт представление страницы.
        /// </summary>
        /// <exception cref="InvalidOperationException">Страница не зарегистрирована.</exception>
        public static BasePage CreatePage(ApplicationPage page, ViewModelBase? viewModel = null)
        {
            if (!_descriptors.TryGetValue(page, out var descriptor))
                throw new InvalidOperationException(
                    "Страница " + page + " не зарегистрирована. Добавьте её в PageRegistry.RegisterDefaults.");

            return descriptor.Factory(viewModel);
        }

        /// <summary>
        /// Возвращает описание страницы либо <c>null</c>, если она не зарегистрирована.
        /// </summary>
        public static PageDescriptor? GetDescriptor(ApplicationPage page)
        {
            return _descriptors.TryGetValue(page, out var descriptor) ? descriptor : null;
        }

        /// <summary>
        /// Определяет страницу по типу представления.
        /// </summary>
        public static ApplicationPage? GetApplicationPage(BasePage page)
        {
            if (page == null)
                return null;

            return _pageTypes.TryGetValue(page.GetType(), out var value) ? value : null;
        }

        /// <summary>
        /// Ось и направление анимации для перехода между двумя страницами.
        /// </summary>
        /// <returns>
        /// <c>Axis</c> — вертикальная, если хотя бы одна из страниц объявлена
        /// вертикальной. <c>Forward</c> — <c>true</c>, если порядок цели не меньше
        /// порядка источника.
        /// </returns>
        public static (PageTransitionAxis Axis, bool Forward) GetTransition(ApplicationPage from, ApplicationPage to)
        {
            var fromDescriptor = GetDescriptor(from);
            var toDescriptor = GetDescriptor(to);

            var axis = (fromDescriptor?.Axis == PageTransitionAxis.Vertical
                        || toDescriptor?.Axis == PageTransitionAxis.Vertical)
                ? PageTransitionAxis.Vertical
                : PageTransitionAxis.Horizontal;

            int fromOrder = fromDescriptor?.Order ?? 0;
            int toOrder = toDescriptor?.Order ?? 0;

            return (axis, toOrder >= fromOrder);
        }
    }
}
