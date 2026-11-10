using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Взаимодействие с пользователем через диалоги.
    /// <para>
    /// Объявлен в Core и не зависит от Avalonia, поэтому модели представления
    /// могут показывать диалоги, оставаясь пригодными для модульных тестов
    /// (в тестах интерфейс подменяется заглушкой).
    /// </para>
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// Показывает сообщение с одной кнопкой подтверждения.
        /// </summary>
        /// <param name="message">Текст сообщения.</param>
        /// <param name="buttonText">
        /// Надпись на кнопке. <c>null</c> — взять из текущего языкового словаря.
        /// </param>
        Task ShowMessageAsync(string message, string? buttonText = null);

        /// <summary>
        /// Показывает запрос «да/нет» с иконками.
        /// </summary>
        /// <returns>
        /// <see cref="MessageBoxResult.Yes"/>, <see cref="MessageBoxResult.No"/>
        /// либо <see cref="MessageBoxResult.None"/>, если диалог закрыли без ответа.
        /// </returns>
        Task<MessageBoxResult> ShowConfirmAsync(string message);

        /// <summary>
        /// Показывает запрос с двумя текстовыми кнопками.
        /// </summary>
        /// <param name="message">Текст вопроса.</param>
        /// <param name="positiveText">Надпись утвердительной кнопки.</param>
        /// <param name="negativeText">Надпись отрицательной кнопки.</param>
        Task<MessageBoxResult> ShowConfirmAsync(string message, string? positiveText, string? negativeText);

        /// <summary>
        /// Показывает запрос с тремя кнопками: да, нет и нейтральный вариант
        /// (например, «Отмена»).
        /// </summary>
        Task<MessageBoxResult> ShowThreefoldAsync(string message, string? positiveText, string? negativeText, string? neutralText);
    }

    /// <summary>
    /// Результат диалога.
    /// </summary>
    public enum MessageBoxResult
    {
        /// <summary>Диалог закрыт без выбора.</summary>
        None = 0,

        /// <summary>Подтверждение.</summary>
        Ok = 1,

        /// <summary>Отмена.</summary>
        Cancel = 2,

        /// <summary>Да.</summary>
        Yes = 6,

        /// <summary>Нет.</summary>
        No = 7,
    }
}
