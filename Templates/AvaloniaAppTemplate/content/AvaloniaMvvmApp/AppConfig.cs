using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp
{
    /// <summary>
    /// Конфигурация приложения: пути к данным, цветовые темы, языки интерфейса
    /// и учёт объектов, требующих освобождения при выходе.
    /// <para>
    /// Темы и языки хранятся в реестрах, а не в фиксированных словарях: добавить
    /// тему или язык можно одной строкой регистрации, без правки этого класса.
    /// </para>
    /// </summary>
    public sealed class AppConfig : IDisposable
    {
        #region Константы и статическое состояние

        /// <summary>Имя файла настроек.</summary>
        public const string SETTINGS_FILE_NAME = "settings.xml";

        /// <summary>Имя файла журнала.</summary>
        public const string LOG_FILE_NAME = "log.txt";

        private const string SETTINGS_DIR_NAME = ".sys";
        private const string REPORTS_DIR_NAME = "Reports";

        private static readonly Lazy<AppConfig> _instance =
            new Lazy<AppConfig>(() => new AppConfig(), isThreadSafe: true);

        /// <summary>
        /// Единственный экземпляр конфигурации.
        /// </summary>
        public static AppConfig GetInstance() => _instance.Value;

        #endregion

        #region Поля

        private readonly HashSet<IDisposable> _objectsForDispose = new HashSet<IDisposable>();
        private readonly object _disposeLock = new object();

        private readonly Dictionary<string, AppResourceOption> _themes =
            new Dictionary<string, AppResourceOption>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, AppResourceOption> _languages =
            new Dictionary<string, AppResourceOption>(StringComparer.OrdinalIgnoreCase);

        private ResourceDictionary? _appliedTheme;
        private ResourceDictionary? _appliedLanguage;

        private ISettingsStore<AppSettings>? _settingsStore;
        private bool _isDisposed;

        #endregion

        #region Свойства

        /// <summary>Имя приложения. Используется в путях к каталогам данных.</summary>
        public string AppName { get; private set; } = "App";

        /// <summary>Имя организации. Используется в путях к каталогам данных.</summary>
        public string CompanyName { get; private set; } = string.Empty;

        /// <summary>Каталог настроек и журнала.</summary>
        public string SettingsDirectoryPath { get; private set; } = string.Empty;

        /// <summary>Каталог отчётов и выгрузок.</summary>
        public string ReportsDirectoryPath { get; private set; } = string.Empty;

        /// <summary>Текущие настройки приложения.</summary>
        public AppSettings Settings { get; private set; } = new AppSettings();

        /// <summary>Ключ активной цветовой темы.</summary>
        public string CurrentThemeKey { get; private set; } = string.Empty;

        /// <summary>Ключ активного языка интерфейса.</summary>
        public string CurrentLanguageKey { get; private set; } = string.Empty;

        /// <summary>Зарегистрированные цветовые темы — для привязки в настройках.</summary>
        public ObservableCollection<AppResourceOption> AvailableThemes { get; } = new ObservableCollection<AppResourceOption>();

        /// <summary>Зарегистрированные языки интерфейса — для привязки в настройках.</summary>
        public ObservableCollection<AppResourceOption> AvailableLanguages { get; } = new ObservableCollection<AppResourceOption>();

        /// <summary>Последний известный размер главного представления.</summary>
        public Size MainViewSize { get; private set; }

        #endregion

        #region События

        /// <summary>Тема изменена. Аргумент — ключ новой темы.</summary>
        public event EventHandler<string>? ThemeChanged;

        /// <summary>Язык изменён. Аргумент — ключ нового языка.</summary>
        public event EventHandler<string>? LanguageChanged;

        /// <summary>Главное представление изменило размер.</summary>
        public event EventHandler<SizeChangedEventArgs>? MainViewSizeChanged;

        /// <summary>Главное представление загружено.</summary>
        public event EventHandler? MainViewLoaded;

        #endregion

        private AppConfig()
        {
        }

        #region Инициализация

        /// <summary>
        /// Готовит конфигурацию к работе: вычисляет пути, создаёт каталоги,
        /// читает настройки и применяет сохранённые тему и язык.
        /// </summary>
        /// <param name="appName">Имя приложения (каталог данных).</param>
        /// <param name="companyName">Имя организации (каталог данных). Может быть пустым.</param>
        /// <param name="settingsStore">
        /// Хранилище настроек. <c>null</c> — XML-файл в каталоге настроек.
        /// </param>
        public void Initialize(string appName, string companyName = "", ISettingsStore<AppSettings>? settingsStore = null)
        {
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentException("Имя приложения не задано.", nameof(appName));

            AppName = appName;
            CompanyName = companyName ?? string.Empty;

            string root = GetPlatformDataRoot();

            SettingsDirectoryPath = BuildPath(root, SETTINGS_DIR_NAME);
            ReportsDirectoryPath = BuildPath(root, REPORTS_DIR_NAME);

            Directory.CreateDirectory(SettingsDirectoryPath);
            Directory.CreateDirectory(ReportsDirectoryPath);

            _settingsStore = settingsStore
                ?? new XmlSettingsStore<AppSettings>(Path.Combine(SettingsDirectoryPath, SETTINGS_FILE_NAME));

            // Повреждённый или отсутствующий файл настроек не должен мешать запуску:
            // берём значения по умолчанию и перезаписываем файл.
            Settings = _settingsStore.Read() ?? new AppSettings();
            _settingsStore.Write(Settings);

            ApplySavedThemeAndLanguage();
        }

        /// <summary>
        /// Возвращает путь к дополнительному каталогу данных приложения,
        /// создавая его при необходимости.
        /// </summary>
        public string GetAppDirectory(string name)
        {
            string path = BuildPath(GetPlatformDataRoot(), name);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Полный путь к файлу журнала.
        /// </summary>
        public string GetLogFilePath() => Path.Combine(SettingsDirectoryPath, LOG_FILE_NAME);

        private string GetPlatformDataRoot()
        {
            if (OperatingSystem.IsWindows())
            {
                // %LOCALAPPDATA% лежит рядом с ApplicationData (%APPDATA%),
                // поэтому берём родительский каталог и спускаемся в Local.
                string? appData = Directory.GetParent(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))?.FullName;

                return Path.Combine(appData ?? Environment.CurrentDirectory, "Local");
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        private string BuildPath(string root, string leaf)
        {
            return string.IsNullOrEmpty(CompanyName)
                ? Path.Combine(root, AppName, leaf)
                : Path.Combine(root, CompanyName, AppName, leaf);
        }

        #endregion

        #region Темы и языки

        /// <summary>
        /// Регистрирует цветовую тему.
        /// </summary>
        /// <param name="key">Ключ темы. Сохраняется в настройках.</param>
        /// <param name="displayNameResourceKey">
        /// Ключ строки в языковом словаре для отображения названия темы.
        /// </param>
        /// <param name="source">Адрес словаря ресурсов (<c>avares://</c>).</param>
        public void RegisterTheme(string key, string displayNameResourceKey, string source)
        {
            Register(_themes, AvailableThemes, key, displayNameResourceKey, source);
        }

        /// <summary>
        /// Регистрирует язык интерфейса.
        /// </summary>
        public void RegisterLanguage(string key, string displayNameResourceKey, string source)
        {
            Register(_languages, AvailableLanguages, key, displayNameResourceKey, source);
        }

        private static void Register(Dictionary<string, AppResourceOption> registry,
                                     ObservableCollection<AppResourceOption> available,
                                     string key,
                                     string displayNameResourceKey,
                                     string source)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Ключ не задан.", nameof(key));

            var option = new AppResourceOption(key, displayNameResourceKey, new Uri(source));

            if (registry.ContainsKey(key))
            {
                // Повторная регистрация заменяет прежнюю: так приложение может
                // переопределить тему, объявленную шаблоном.
                registry[key] = option;

                for (int i = 0; i < available.Count; i++)
                {
                    if (string.Equals(available[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        available[i] = option;
                        return;
                    }
                }
            }

            registry[key] = option;
            available.Add(option);
        }

        /// <summary>
        /// Применяет тему и сохраняет выбор в настройках.
        /// </summary>
        /// <returns><c>true</c>, если тема найдена и применена.</returns>
        public bool ApplyTheme(string key)
        {
            if (!_themes.TryGetValue(key, out var option))
                return false;

            _appliedTheme = SwapDictionary(_appliedTheme, option.Source);
            CurrentThemeKey = option.Key;

            Settings.ThemeKey = option.Key;
            SaveSettings();

            ThemeChanged?.Invoke(this, option.Key);
            return true;
        }

        /// <summary>
        /// Применяет язык интерфейса и сохраняет выбор в настройках.
        /// </summary>
        /// <returns><c>true</c>, если язык найден и применён.</returns>
        public bool ApplyLanguage(string key)
        {
            if (!_languages.TryGetValue(key, out var option))
                return false;

            _appliedLanguage = SwapDictionary(_appliedLanguage, option.Source);
            CurrentLanguageKey = option.Key;

            Settings.LanguageKey = option.Key;
            SaveSettings();

            LanguageChanged?.Invoke(this, option.Key);
            return true;
        }

        /// <summary>
        /// Применяет тему и язык из настроек. Если сохранённый ключ неизвестен,
        /// берётся первый зарегистрированный вариант.
        /// </summary>
        public void ApplySavedThemeAndLanguage()
        {
            if (!ApplyTheme(Settings.ThemeKey) && AvailableThemes.Count > 0)
                ApplyTheme(AvailableThemes[0].Key);

            if (!ApplyLanguage(Settings.LanguageKey) && AvailableLanguages.Count > 0)
                ApplyLanguage(AvailableLanguages[0].Key);
        }

        /// <summary>
        /// Заменяет подключённый словарь ресурсов новым.
        /// </summary>
        private static ResourceDictionary? SwapDictionary(ResourceDictionary? previous, Uri source)
        {
            var application = Application.Current;
            if (application == null)
                return previous;

            var merged = application.Resources.MergedDictionaries;

            if (previous != null)
                merged.Remove(previous);

            var loaded = (ResourceDictionary)AvaloniaXamlLoader.Load(source);
            merged.Add(loaded);

            return loaded;
        }

        #endregion

        #region Настройки

        /// <summary>
        /// Сохраняет настройки в хранилище.
        /// </summary>
        public bool SaveSettings()
        {
            return _settingsStore?.Write(Settings) ?? false;
        }

        #endregion

        #region Жизненный цикл

        /// <summary>
        /// Подключается к жизненному циклу настольного приложения,
        /// чтобы освободить ресурсы при выходе.
        /// </summary>
        public void SetApplicationLifetime(IControlledApplicationLifetime lifetime)
        {
            lifetime.Exit += (_, _) => Dispose();
        }

        /// <summary>
        /// Подключается к главному представлению однооконной платформы
        /// (Android, iOS, браузер), чтобы освободить ресурсы при его выгрузке.
        /// <para>
        /// Подключение идёт к самому представлению, а не к жизненному циклу:
        /// на Android в Avalonia 12 представление создаёт фабрика
        /// <see cref="IActivityApplicationLifetime.MainViewFactory"/>, и к моменту
        /// настройки жизненного цикла представления ещё не существует.
        /// </para>
        /// </summary>
        public void AttachToMainView(Control mainView)
        {
            if (mainView != null)
                mainView.Unloaded += (_, _) => Dispose();
        }

        /// <summary>
        /// Регистрирует объект для освобождения при выходе из приложения.
        /// </summary>
        public void RegisterDisposableObject(IDisposable disposable)
        {
            if (disposable == null)
                return;

            lock (_disposeLock)
            {
                _objectsForDispose.Add(disposable);
            }
        }

        /// <summary>
        /// Снимает объект с учёта.
        /// <para>
        /// Вызывайте для короткоживущих объектов (страниц, окон, моделей
        /// представления), которые освобождаются раньше выхода из приложения:
        /// иначе они останутся в списке и не будут собраны сборщиком мусора.
        /// </para>
        /// </summary>
        public void UnregisterDisposableObject(IDisposable disposable)
        {
            if (disposable == null)
                return;

            lock (_disposeLock)
            {
                _objectsForDispose.Remove(disposable);
            }
        }

        /// <summary>
        /// Уведомляет об изменении размера главного представления.
        /// </summary>
        public void NotifyMainViewSizeChanged(object sender, SizeChangedEventArgs args)
        {
            MainViewSize = args.NewSize;
            MainViewSizeChanged?.Invoke(sender, args);
        }

        /// <summary>
        /// Уведомляет о загрузке главного представления.
        /// </summary>
        public void NotifyMainViewLoaded(object sender)
        {
            MainViewLoaded?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Освобождает все учтённые объекты. Повторный вызов безопасен.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            IDisposable[] objects;
            lock (_disposeLock)
            {
                objects = new IDisposable[_objectsForDispose.Count];
                _objectsForDispose.CopyTo(objects);
                _objectsForDispose.Clear();
            }

            foreach (var obj in objects)
            {
                try
                {
                    obj.Dispose();
                }
                catch (Exception ex)
                {
                    // Сбой освобождения одного объекта не должен мешать остальным.
                    System.Diagnostics.Debug.WriteLine("Ошибка освобождения " + obj.GetType().Name + ": " + ex.Message);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Зарегистрированный словарь ресурсов (тема либо язык).
    /// </summary>
    /// <param name="Key">Ключ, сохраняемый в настройках.</param>
    /// <param name="DisplayNameResourceKey">
    /// Ключ строки в языковом словаре с отображаемым названием.
    /// </param>
    /// <param name="Source">Адрес словаря ресурсов.</param>
    public sealed record AppResourceOption(string Key, string DisplayNameResourceKey, Uri Source);

    /// <summary>
    /// Ключи тем, поставляемых шаблоном.
    /// </summary>
    public static class AppThemes
    {
        public const string Light = "Light";
        public const string Dark = "Dark";
    }

    /// <summary>
    /// Ключи языков, поставляемых шаблоном.
    /// </summary>
    public static class AppLanguages
    {
        public const string English = "en-US";
        public const string Russian = "ru-RU";
    }
}
