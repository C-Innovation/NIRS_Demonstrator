using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaMvvmApp.Core;

namespace AvaloniaMvvmApp.ViewModels
{
    /// <summary>
    /// Модель представления главной страницы.
    /// Служит примером: команды навигации, диалог и запись в журнал.
    /// </summary>
    public class MainPageViewModel : ViewModelBase
    {
        /// <summary>
        /// Переход на страницу настроек.
        /// </summary>
        public ICommand GoToSettingsCommand { get; }

        /// <summary>
        /// Показ диалога — проверка, что <see cref="IUIManager"/> подключён.
        /// </summary>
        public ICommand ShowDialogCommand { get; }

        private string _statusText = string.Empty;

        /// <summary>
        /// Строка состояния под кнопками.
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public MainPageViewModel()
        {
            GoToSettingsCommand = new RelayCommand(() => IoC.Application.GoToPage(ApplicationPage.Settings));

            // Асинхронная команда вместо async void: ошибка внутри
            // попадёт в журнал, а не в необработанные исключения.
            ShowDialogCommand = new AsyncRelayCommand(ShowDialogAsync);
        }

        private async Task ShowDialogAsync()
        {
            string question = AppResources.GetString("DialogQuestionText", "Close this dialog?");

            MessageBoxResult result = await IoC.UI.ShowConfirmAsync(question);

            StatusText = result switch
            {
                MessageBoxResult.Yes => AppResources.GetString("YesText", "Yes"),
                MessageBoxResult.No => AppResources.GetString("NoText", "No"),
                _ => AppResources.GetString("CancelText", "Cancel"),
            };

            IoC.Logger.Log("Диалог закрыт с результатом " + result, LogLevel.Informative);
        }
    }
}
