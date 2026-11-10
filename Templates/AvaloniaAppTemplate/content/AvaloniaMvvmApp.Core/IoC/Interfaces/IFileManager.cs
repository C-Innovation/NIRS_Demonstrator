using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Операции с файлами, безопасные при конкурентном доступе.
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Записывает текст в файл. Одновременные записи в один файл
        /// выстраиваются в очередь, а не конфликтуют.
        /// </summary>
        /// <param name="text">Записываемый текст.</param>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="append">Дописать в конец вместо перезаписи.</param>
        Task WriteTextToFileAsync(string text, string path, bool append = false);

        /// <summary>
        /// Читает текст файла. Возвращает <c>null</c>, если файла нет.
        /// </summary>
        Task<string?> ReadTextFromFileAsync(string path);

        /// <summary>
        /// Приводит разделители пути к принятым в текущей ОС.
        /// </summary>
        string NormalizePath(string path);

        /// <summary>
        /// Разворачивает путь в абсолютный.
        /// </summary>
        string ResolvePath(string path);
    }
}
