namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Хранилище настроек приложения.
    /// <para>
    /// Вынесено в интерфейс, чтобы формат хранения можно было заменить
    /// (XML по умолчанию, JSON, реестр, удалённый профиль), не трогая
    /// <c>AppConfig</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="TSettings">Тип модели настроек.</typeparam>
    public interface ISettingsStore<TSettings> where TSettings : class, new()
    {
        /// <summary>
        /// Читает настройки. Возвращает <c>null</c>, если хранилище
        /// пусто или повреждено — вызывающая сторона решает, что делать.
        /// </summary>
        TSettings? Read();

        /// <summary>
        /// Записывает настройки.
        /// </summary>
        /// <returns><c>true</c>, если запись удалась.</returns>
        bool Write(TSettings settings);
    }
}
