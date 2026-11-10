using System;
using System.IO;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Пишет сообщения в файл.
    /// <para>
    /// Запись выполняется самим классом, а не через <see cref="IFileManager"/>:
    /// журнал создаётся на самом раннем этапе запуска, когда контейнер
    /// зависимостей ещё может быть не собран.
    /// </para>
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly object _writeLock = new object();

        /// <summary>
        /// Путь к файлу журнала.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Добавлять ли метку времени к каждой строке.
        /// </summary>
        public bool LogTime { get; set; } = true;

        public FileLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Путь к файлу журнала не задан.", nameof(filePath));

            FilePath = filePath;

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <inheritdoc />
        public void Log(string message, LogLevel level)
        {
            string timeStamp = LogTime
                ? "[" + DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] "
                : string.Empty;

            string line = timeStamp + "[" + level + "] " + message + Environment.NewLine;

            try
            {
                // Журнал не должен ронять приложение, поэтому запись синхронная
                // под замком, а ошибки проглатываются осознанно.
                lock (_writeLock)
                {
                    File.AppendAllText(FilePath, line);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
