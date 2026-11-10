using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp
{
    /// <summary>
    /// Базовая страница приложения без привязки к типу модели представления.
    /// Используйте обобщённую версию <see cref="BasePage{VM}"/>.
    /// </summary>
    public abstract class BasePage : UserControl
    {
        private object? _viewModel;

        /// <summary>
        /// Модель представления страницы.
        /// При установке становится <see cref="Control.DataContext"/>.
        /// </summary>
        public object? ViewModelObject
        {
            get => _viewModel;
            set
            {
                if (ReferenceEquals(_viewModel, value))
                    return;

                _viewModel = value;
                DataContext = _viewModel;

                OnViewModelChanged();
            }
        }

        protected BasePage()
        {
            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        /// <summary>
        /// Вызывается после смены модели представления.
        /// </summary>
        protected virtual void OnViewModelChanged()
        {
        }

        /// <summary>
        /// Страница появилась на экране.
        /// <para>
        /// Здесь уместно подписываться на события и запускать обновление данных.
        /// Парный метод — <see cref="OnPageUnloaded"/>.
        /// </para>
        /// </summary>
        protected virtual void OnPageLoaded()
        {
        }

        /// <summary>
        /// Страница убрана с экрана.
        /// <para>
        /// Обязательно отписывайтесь здесь от всего, на что подписались
        /// в <see cref="OnPageLoaded"/>: иначе страница останется достижимой
        /// из долгоживущих объектов и не будет собрана сборщиком мусора.
        /// </para>
        /// </summary>
        protected virtual void OnPageUnloaded()
        {
        }

        private void HandleLoaded(object? sender, RoutedEventArgs e) => OnPageLoaded();

        private void HandleUnloaded(object? sender, RoutedEventArgs e) => OnPageUnloaded();
    }

    /// <summary>
    /// Базовая страница с типизированной моделью представления.
    /// </summary>
    /// <typeparam name="VM">Тип модели представления.</typeparam>
    public abstract class BasePage<VM> : BasePage
        where VM : ViewModelBase
    {
        /// <summary>
        /// Создала ли страница модель представления сама. Только такую модель
        /// страница вправе освободить: переданную извне освобождает её владелец.
        /// </summary>
        private readonly bool _ownsViewModel;

        /// <summary>
        /// Типизированная модель представления страницы.
        /// </summary>
        public VM ViewModel
        {
            get => (VM)ViewModelObject!;
            set => ViewModelObject = value;
        }

        /// <param name="specificViewModel">
        /// Готовая модель представления. Если <c>null</c>, страница получит
        /// модель из контейнера зависимостей.
        /// </param>
        protected BasePage(VM? specificViewModel = null)
        {
            if (specificViewModel != null)
            {
                ViewModel = specificViewModel;
                _ownsViewModel = false;
            }
            else
            {
                ViewModel = IoC.Get<VM>();
                _ownsViewModel = true;
            }
        }

        /// <inheritdoc />
        protected override void OnPageUnloaded()
        {
            base.OnPageUnloaded();

            if (_ownsViewModel && ViewModelObject is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
