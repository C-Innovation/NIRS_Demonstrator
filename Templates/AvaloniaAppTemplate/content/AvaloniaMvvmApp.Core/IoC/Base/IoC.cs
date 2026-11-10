using System;
using Microsoft.Extensions.DependencyInjection;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Статический фасад над контейнером внедрения зависимостей.
    /// <para>
    /// Контейнер — Microsoft.Extensions.DependencyInjection, единственный в решении.
    /// Фасад даёт короткий доступ к службам из XAML-привязок и из кода, где
    /// внедрение через конструктор неудобно (например, в code-behind элементов).
    /// В новом коде предпочитайте внедрение через конструктор, а фасад —
    /// для точек, куда контейнер не достаёт.
    /// </para>
    /// </summary>
    public static class IoC
    {
        private static IServiceProvider? _provider;

        /// <summary>
        /// Провайдер служб. Доступен после <see cref="Setup"/>.
        /// </summary>
        public static IServiceProvider Provider =>
            _provider ?? throw new InvalidOperationException(
                $"Контейнер не инициализирован. Вызовите {nameof(IoC)}.{nameof(Setup)} при запуске приложения.");

        /// <summary>
        /// Инициализирован ли контейнер.
        /// </summary>
        public static bool IsConfigured => _provider != null;

        #region Быстрый доступ к базовым службам

        /// <summary>Журналирование.</summary>
        public static ILogFactory Logger => Get<ILogFactory>();

        /// <summary>Работа с файлами.</summary>
        public static IFileManager File => Get<IFileManager>();

        /// <summary>Запуск фоновых задач с протоколированием ошибок.</summary>
        public static ITaskManager Task => Get<ITaskManager>();

        /// <summary>Диалоги и прочее взаимодействие с пользователем.</summary>
        public static IUIManager UI => Get<IUIManager>();

        /// <summary>Навигация по страницам приложения.</summary>
        public static ApplicationViewModel Application => Get<ApplicationViewModel>();

        #endregion

        /// <summary>
        /// Собирает контейнер. Вызывается один раз при запуске приложения.
        /// </summary>
        /// <param name="configure">
        /// Регистрация служб конкретного приложения. Базовые службы
        /// (журнал, файлы, задачи, навигация) регистрируются до этого вызова,
        /// поэтому их можно заменить своими реализациями.
        /// </param>
        public static void Setup(Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();

            RegisterCoreServices(services);
            configure?.Invoke(services);

            _provider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Возвращает службу из контейнера.
        /// <para>
        /// Если тип не зарегистрирован, создаёт его через контейнер
        /// (<see cref="ActivatorUtilities"/>), внедряя зарегистрированные зависимости.
        /// Благодаря этому модели представления страниц работают без обязательной
        /// регистрации каждой из них.
        /// </para>
        /// </summary>
        public static T Get<T>() where T : class
        {
            return Provider.GetService<T>() ?? ActivatorUtilities.CreateInstance<T>(Provider);
        }

        /// <summary>
        /// Возвращает службу или <c>null</c>, если она не зарегистрирована.
        /// </summary>
        public static T? GetOrNull<T>() where T : class
        {
            return _provider?.GetService<T>();
        }

        /// <summary>
        /// Освобождает контейнер. Для тестов и корректного завершения приложения.
        /// </summary>
        public static void Teardown()
        {
            if (_provider is IDisposable disposable)
                disposable.Dispose();

            _provider = null;
        }

        /// <summary>
        /// Регистрации, общие для любого приложения на этом шаблоне.
        /// Все — через <c>TryAdd</c>, чтобы приложение могло подменить реализацию.
        /// </summary>
        private static void RegisterCoreServices(IServiceCollection services)
        {
            services.TryAddSingletonService<ApplicationViewModel>(new ApplicationViewModel());
            services.TryAddSingletonService<IFileManager>(new FileManager());
            services.TryAddSingletonService<ITaskManager>(new TaskManager());
        }
    }

    /// <summary>
    /// Небольшие расширения, чтобы не тянуть пакет
    /// Microsoft.Extensions.DependencyInjection.Abstractions ради <c>TryAdd</c>.
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        internal static void TryAddSingletonService<TService>(this IServiceCollection services, TService instance)
            where TService : class
        {
            foreach (var descriptor in services)
            {
                if (descriptor.ServiceType == typeof(TService))
                    return;
            }

            services.AddSingleton(instance);
        }
    }
}
