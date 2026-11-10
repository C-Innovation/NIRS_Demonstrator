using System;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Выводит сообщения в консоль, раскрашивая их по уровню.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <inheritdoc />
        public void Log(string message, LogLevel level)
        {
            var previousColor = Console.ForegroundColor;

            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Blue,
                LogLevel.Verbose => ConsoleColor.Gray,
                LogLevel.Warning => ConsoleColor.DarkYellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Success => ConsoleColor.Green,
                _ => ConsoleColor.White,
            };

            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }
    }
}
