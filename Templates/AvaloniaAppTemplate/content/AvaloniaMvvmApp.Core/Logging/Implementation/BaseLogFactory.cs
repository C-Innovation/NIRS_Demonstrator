using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Журнал по умолчанию: рассылает сообщение всем подключённым получателям.
    /// Всегда включает <see cref="DebugLogger"/>, остальные передаются в конструктор.
    /// </summary>
    public class BaseLogFactory : ILogFactory
    {
        private readonly List<ILogger> _loggers = new List<ILogger>();
        private readonly object _loggersLock = new object();

        /// <inheritdoc />
        public LogOutputLevel LogOutputLevel { get; set; } = LogOutputLevel.Debug;

        /// <inheritdoc />
        public bool IncludeLogOriginDetails { get; set; } = true;

        /// <inheritdoc />
        public event Action<(string Message, LogLevel Level)> NewLog = _ => { };

        /// <param name="loggers">Дополнительные получатели сообщений.</param>
        public BaseLogFactory(ILogger[]? loggers = null)
        {
            AddLogger(new DebugLogger());

            if (loggers == null)
                return;

            foreach (var logger in loggers)
                AddLogger(logger);
        }

        /// <inheritdoc />
        public void AddLogger(ILogger logger)
        {
            lock (_loggersLock)
            {
                if (!_loggers.Contains(logger))
                    _loggers.Add(logger);
            }
        }

        /// <inheritdoc />
        public void RemoveLogger(ILogger logger)
        {
            lock (_loggersLock)
            {
                _loggers.Remove(logger);
            }
        }

        /// <inheritdoc />
        public void Log(string message,
                        LogLevel level = LogLevel.Informative,
                        [CallerMemberName] string origin = "",
                        [CallerFilePath] string filePath = "",
                        [CallerLineNumber] int lineNumber = 0)
        {
            if ((int)level < (int)LogOutputLevel)
                return;

            if (IncludeLogOriginDetails)
                message = message + " [" + Path.GetFileName(filePath) + " > " + origin + " > строка " + lineNumber + "]";

            lock (_loggersLock)
            {
                foreach (var logger in _loggers)
                    logger.Log(message, level);
            }

            NewLog.Invoke((message, level));
        }
    }
}
