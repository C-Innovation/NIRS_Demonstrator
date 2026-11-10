using Avalonia.Threading;
using AvaloniaDialogs.Views;
using System;
using System.Threading.Tasks;
using AvaloniaMvvmApp.Core;
using AvaloniaMvvmApp.Dialogs;

namespace AvaloniaMvvmApp
{
    /// <summary>
    /// Реализация <see cref="IUIManager"/> на DialogHost.
    /// <para>
    /// Диалог показывается внутри <c>DialogHost</c> из <c>MainView</c> — отдельных
    /// окон не создаётся, поэтому работает одинаково на настольных и однооконных
    /// платформах (мобильные устройства, браузер).
    /// </para>
    /// </summary>
    public class UIManager : IUIManager
    {
        /// <inheritdoc />
        public Task ShowMessageAsync(string message, string? buttonText = null)
        {
            return OnUiThreadAsync(async () =>
            {
                var dialog = new MessageDialog
                {
                    Message = message,
                    ButtonText = buttonText ?? AppResources.GetString("OkText", "OK"),
                };

                await dialog.ShowAsync();
                return MessageBoxResult.Ok;
            });
        }

        /// <inheritdoc />
        public Task<MessageBoxResult> ShowConfirmAsync(string message)
        {
            return OnUiThreadAsync(async () =>
            {
                var dialog = new ConfirmDialog { Message = message };
                var result = await dialog.ShowAsync();

                // Диалог могли закрыть мимо кнопок (клавишей Escape) —
                // тогда значения нет и ответ считается неполученным.
                return result.HasValue ? result.Value : MessageBoxResult.None;
            });
        }

        /// <inheritdoc />
        public Task<MessageBoxResult> ShowConfirmAsync(string message, string? positiveText, string? negativeText)
        {
            return ShowChoiceAsync(message, positiveText, negativeText, neutralText: null);
        }

        /// <inheritdoc />
        public Task<MessageBoxResult> ShowThreefoldAsync(string message, string? positiveText, string? negativeText, string? neutralText)
        {
            return ShowChoiceAsync(message,
                                   positiveText,
                                   negativeText,
                                   neutralText ?? AppResources.GetString("CancelText", "Cancel"));
        }

        /// <summary>
        /// Показывает произвольный диалог шаблона и возвращает его результат.
        /// <para>
        /// Точка расширения для диалогов приложения: наследуйте
        /// <see cref="BaseDialog{TResult}"/> и показывайте через этот метод,
        /// не дублируя работу с потоком интерфейса.
        /// </para>
        /// </summary>
        public Task<TResult?> ShowDialogAsync<TResult>(BaseDialog<TResult> dialog)
        {
            if (dialog == null)
                throw new ArgumentNullException(nameof(dialog));

            return OnUiThreadAsync<TResult?>(async () =>
            {
                var result = await dialog.ShowAsync();
                return result.HasValue ? result.Value : default;
            });
        }

        private static Task<MessageBoxResult> ShowChoiceAsync(string message,
                                                              string? positiveText,
                                                              string? negativeText,
                                                              string? neutralText)
        {
            return OnUiThreadAsync(async () =>
            {
                var dialog = new ChoiceDialog
                {
                    Message = message,
                    PositiveText = positiveText ?? AppResources.GetString("YesText", "Yes"),
                    NegativeText = negativeText ?? AppResources.GetString("NoText", "No"),
                    NeutralText = neutralText,
                };

                var result = await dialog.ShowAsync();
                return result.HasValue ? result.Value : MessageBoxResult.None;
            });
        }

        /// <summary>
        /// Выполняет работу в потоке интерфейса.
        /// <para>
        /// Модель представления вправе вызвать диалог из фоновой задачи,
        /// поэтому переход в поток интерфейса делается здесь, а не на каждом
        /// месте вызова.
        /// </para>
        /// </summary>
        private static async Task<T> OnUiThreadAsync<T>(Func<Task<T>> action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                return await action();

            var completion = new TaskCompletionSource<T>();

            Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    completion.SetResult(await action());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            });

            return await completion.Task;
        }
    }
}
