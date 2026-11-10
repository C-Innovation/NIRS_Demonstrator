using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.ViewModels
{
    /// <summary>
    /// Точка доступа к долгоживущим моделям представления из XAML.
    /// <para>
    /// Нужен там, где у элемента свой <c>DataContext</c>, но требуется
    /// добраться до состояния приложения. В XAML используется так:
    /// <c>{Binding ApplicationViewModel.CurrentPage,
    /// Source={x:Static vm:ViewModelLocator.Instance}}</c>.
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Единственный экземпляр локатора.
        /// </summary>
        public static ViewModelLocator Instance { get; } = new ViewModelLocator();

        /// <summary>
        /// Модель представления приложения: текущая страница и навигация.
        /// </summary>
        public ApplicationViewModel ApplicationViewModel => IoC.Application;

        /// <summary>
        /// Конфигурация приложения: темы, языки, пути к данным.
        /// Доступна из XAML для привязки заголовков и настроек.
        /// </summary>
        public AppConfig AppConfig => AppConfig.GetInstance();
    }
}
