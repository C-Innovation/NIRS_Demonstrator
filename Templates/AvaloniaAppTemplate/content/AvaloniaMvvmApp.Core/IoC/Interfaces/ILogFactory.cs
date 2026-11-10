using System;
using System.Runtime.CompilerServices;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Журнал приложения: рассылает сообщение всем подключённым получателям.
    /// </summary>
    public interface ILogFactory
    {
        /// <summary>
        /// Возникает на каждое записанное сообщение. Удобно, чтобы выводить
        /// журнал в интерфейсе приложения.
        /// </summary>
        event Action<(string Message, LogLevel Level)> NewLog;

        /// <summary>
        /// Минимальный уровень, начиная с которого сообщения записываются.
        /// </summary>
        LogOutputLevel LogOutputLevel { get; set; }

        /// <summary>
        /// Добавлять ли к сообщению файл, метод и строку источника.
        /// </summary>
        bool IncludeLogOriginDetails { get; set; }

        /// <summary>
        /// Подключает получателя сообщений.
        /// </summary>
        void AddLogger(ILogger logger);

        /// <summary>
        /// Отключает получателя сообщений.
        /// </summary>
        void RemoveLogger(ILogger logger);

        /// <summary>
        /// Записывает сообщение. Аргументы источника подставляет компилятор.
        /// </summary>
        void Log(string message,
                 LogLevel level = LogLevel.Informative,
                 [CallerMemberName] string origin = "",
                 [CallerFilePath] string filePath = "",
                 [CallerLineNumber] int lineNumber = 0);
    }
}
