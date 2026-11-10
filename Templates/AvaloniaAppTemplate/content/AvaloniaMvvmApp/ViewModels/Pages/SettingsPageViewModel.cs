using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.ViewModels
{
    /// <summary>
    /// Выбираемый вариант темы или языка.
    /// <para>
    /// Отображаемое название берётся из языкового словаря, поэтому при смене
    /// языка его нужно перечитать — для этого есть <see cref="RefreshDisplayName"/>.
    /// </para>
    /// </summary>
    public class AppOptionItem : ViewModelBase
    {
        /// <summary>
        /// Описание варианта из реестра <see cref="AppConfig"/>.
        /// </summary>
        public AppResourceOption Option { get; }

        /// <summary>
        /// Название для показа в списке.
        /// </summary>
        public string DisplayName => AppResources.GetString(Option.DisplayNameResourceKey, Option.Key);

        public AppOptionItem(AppResourceOption option)
        {
            Option = option;
        }

        /// <summary>
        /// Перечитывает название после смены языка.
        /// </summary>
        public void RefreshDisplayName() => OnPropertyChanged(nameof(DisplayName));
    }

    /// <summary>
    /// Модель представления страницы настроек: выбор темы и языка интерфейса.
    /// </summary>
    public class SettingsPageViewModel : ViewModelBase
    {
        private readonly AppConfig _config = AppConfig.GetInstance();

        /// <summary>
        /// Доступные цветовые темы.
        /// </summary>
        public ObservableCollection<AppOptionItem> Themes { get; } = new ObservableCollection<AppOptionItem>();

        /// <summary>
        /// Доступные языки интерфейса.
        /// </summary>
        public ObservableCollection<AppOptionItem> Languages { get; } = new ObservableCollection<AppOptionItem>();

        private AppOptionItem? _selectedTheme;

        /// <summary>
        /// Выбранная тема. Применяется и сохраняется сразу при выборе.
        /// </summary>
        public AppOptionItem? SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (!SetProperty(ref _selectedTheme, value) || value == null)
                    return;

                _config.ApplyTheme(value.Option.Key);
            }
        }

        private AppOptionItem? _selectedLanguage;

        /// <summary>
        /// Выбранный язык. Применяется и сохраняется сразу при выборе.
        /// </summary>
        public AppOptionItem? SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (!SetProperty(ref _selectedLanguage, value) || value == null)
                    return;

                _config.ApplyLanguage(value.Option.Key);
            }
        }

        /// <summary>
        /// Возврат на предыдущую страницу.
        /// </summary>
        public ICommand GoBackCommand { get; }

        public SettingsPageViewModel()
        {
            GoBackCommand = new RelayCommand(() =>
            {
                if (!IoC.Application.GoBack())
                    IoC.Application.GoToPage(ApplicationPage.Main);
            });

            foreach (var theme in _config.AvailableThemes)
                Themes.Add(new AppOptionItem(theme));

            foreach (var language in _config.AvailableLanguages)
                Languages.Add(new AppOptionItem(language));

            _selectedTheme = Find(Themes, _config.CurrentThemeKey);
            _selectedLanguage = Find(Languages, _config.CurrentLanguageKey);

            _config.LanguageChanged += OnLanguageChanged;
        }

        private static AppOptionItem? Find(ObservableCollection<AppOptionItem> items, string key)
        {
            foreach (var item in items)
            {
                if (string.Equals(item.Option.Key, key, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        private void OnLanguageChanged(object? sender, string languageKey)
        {
            // Названия тем и языков живут в языковом словаре,
            // поэтому после переключения их нужно перечитать.
            foreach (var item in Themes)
                item.RefreshDisplayName();

            foreach (var item in Languages)
                item.RefreshDisplayName();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            // Без отписки модель представления осталась бы достижимой
            // из AppConfig, который живёт всё время работы приложения.
            _config.LanguageChanged -= OnLanguageChanged;
            base.Dispose(disposing);
        }
    }
}
