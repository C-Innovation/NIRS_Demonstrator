using System;
using System.Xml.Serialization;

namespace AvaloniaMvvmApp
{
    /// <summary>
    /// Сохраняемые настройки приложения.
    /// <para>
    /// Добавляя своё поле: объявите свойство с <see cref="XmlElementAttribute"/>
    /// и задайте значение по умолчанию в конструкторе. Старые файлы настроек
    /// читаются без ошибок — отсутствующие элементы остаются со значением
    /// по умолчанию.
    /// </para>
    /// </summary>
    [Serializable]
    [XmlRoot("AppSettings")]
    [XmlType("AppSettings")]
    public class AppSettings
    {
        /// <summary>
        /// Версия приложения, записавшего файл. Помогает при миграции настроек.
        /// </summary>
        [XmlAttribute("Version")]
        public string AppVersion { get; set; }

        /// <summary>
        /// Ключ выбранной цветовой темы.
        /// </summary>
        [XmlElement("ThemeKey")]
        public string ThemeKey { get; set; }

        /// <summary>
        /// Ключ выбранного языка интерфейса.
        /// </summary>
        [XmlElement("LanguageKey")]
        public string LanguageKey { get; set; }

        public AppSettings()
        {
            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";
            ThemeKey = AppThemes.Light;
            LanguageKey = AppLanguages.English;
        }
    }
}
