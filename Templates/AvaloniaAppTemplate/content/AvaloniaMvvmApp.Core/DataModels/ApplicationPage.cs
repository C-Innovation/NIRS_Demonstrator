namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Страницы приложения.
    /// <para>
    /// Добавляя страницу: допишите значение здесь и зарегистрируйте фабрику
    /// в <c>PageRegistry.RegisterDefaults</c> — больше править ничего не нужно,
    /// переходы и анимация берутся из регистрации.
    /// </para>
    /// </summary>
    public enum ApplicationPage
    {
        /// <summary>
        /// Главная страница. Точка отсчёта для направления анимации перехода.
        /// </summary>
        Main = 0,

        /// <summary>
        /// Страница настроек.
        /// </summary>
        Settings = 1,
    }
}
