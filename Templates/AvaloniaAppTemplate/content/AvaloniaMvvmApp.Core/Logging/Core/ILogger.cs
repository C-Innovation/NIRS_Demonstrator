namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Получатель сообщений журнала (файл, консоль, окно отладки, своя реализация).
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Записывает сообщение.
        /// </summary>
        void Log(string message, LogLevel level);
    }
}
