using System.Diagnostics;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Выводит сообщения в окно отладки.
    /// </summary>
    public class DebugLogger : ILogger
    {
        /// <inheritdoc />
        public void Log(string message, LogLevel level)
        {
            string category = level switch
            {
                LogLevel.Debug => "information",
                LogLevel.Verbose => "verbose",
                LogLevel.Warning => "warning",
                LogLevel.Error => "error",
                LogLevel.Success => "success",
                _ => "information",
            };

            Debug.WriteLine(message, category);
        }
    }
}
