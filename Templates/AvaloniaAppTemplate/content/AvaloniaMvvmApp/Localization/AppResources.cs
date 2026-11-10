using Avalonia;
using Avalonia.Styling;

namespace AvaloniaMvvmApp
{
    /// <summary>
    /// Доступ к строкам текущего языкового словаря из кода.
    /// <para>
    /// В XAML строки берутся через <c>{DynamicResource ИмяКлюча}</c>. Этот класс
    /// нужен там, где строка собирается в коде — в диалогах и сообщениях журнала.
    /// </para>
    /// </summary>
    public static class AppResources
    {
        /// <summary>
        /// Возвращает строку по ключу из словарей приложения.
        /// </summary>
        /// <param name="key">Ключ ресурса.</param>
        /// <param name="fallback">
        /// Значение, если ключ не найден. Так приложение не падает
        /// из-за незаполненного перевода.
        /// </param>
        public static string GetString(string key, string fallback = "")
        {
            object? value = GetResource(key);
            return value as string ?? fallback;
        }

        /// <summary>
        /// Возвращает ресурс по ключу либо <c>null</c>.
        /// </summary>
        public static object? GetResource(string key)
        {
            var application = Application.Current;
            if (application == null || string.IsNullOrEmpty(key))
                return null;

            return application.Resources.TryGetResource(key, application.ActualThemeVariant, out object? value)
                ? value
                : null;
        }
    }
}
