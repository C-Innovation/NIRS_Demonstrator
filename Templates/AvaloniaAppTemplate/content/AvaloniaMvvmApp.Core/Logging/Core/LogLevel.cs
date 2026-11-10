namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Важность сообщения журнала.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>Отладочное сообщение.</summary>
        Debug = 1,

        /// <summary>Подробное сообщение.</summary>
        Verbose = 2,

        /// <summary>Информационное сообщение.</summary>
        Informative = 3,

        /// <summary>Предупреждение.</summary>
        Warning = 4,

        /// <summary>Ошибка.</summary>
        Error = 5,

        /// <summary>Успешно выполненная операция.</summary>
        Success = 6,
    }

    /// <summary>
    /// Порог, начиная с которого сообщения попадают в журнал.
    /// </summary>
    public enum LogOutputLevel
    {
        /// <summary>Записывать всё.</summary>
        Debug = 1,

        /// <summary>Подробно и выше.</summary>
        Verbose = 2,

        /// <summary>Информационные и выше.</summary>
        Informative = 3,

        /// <summary>Только предупреждения и ошибки.</summary>
        Critical = 4,

        /// <summary>Ничего не записывать.</summary>
        Nothing = 7,
    }
}
