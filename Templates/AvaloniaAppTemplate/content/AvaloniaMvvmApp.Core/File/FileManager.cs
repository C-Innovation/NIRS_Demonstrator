using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Реализация <see cref="IFileManager"/> по умолчанию.
    /// Доступ к одному и тому же файлу сериализуется через <see cref="AsyncAwaiter"/>.
    /// </summary>
    public class FileManager : IFileManager
    {
        /// <inheritdoc />
        public async Task WriteTextToFileAsync(string text, string path, bool append = false)
        {
            path = ResolvePath(NormalizePath(path));

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // Ключ — путь к файлу: записи в разные файлы идут параллельно,
            // записи в один файл выстраиваются в очередь.
            await AsyncAwaiter.AwaitAsync(nameof(FileManager) + path, async () =>
            {
                if (append)
                    await System.IO.File.AppendAllTextAsync(path, text);
                else
                    await System.IO.File.WriteAllTextAsync(path, text);
            });
        }

        /// <inheritdoc />
        public async Task<string?> ReadTextFromFileAsync(string path)
        {
            path = ResolvePath(NormalizePath(path));

            if (!System.IO.File.Exists(path))
                return null;

            return await AsyncAwaiter.AwaitResultAsync<string?>(nameof(FileManager) + path,
                async () => await System.IO.File.ReadAllTextAsync(path));
        }

        /// <inheritdoc />
        public string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? path.Replace('/', '\\').Trim()
                : path.Replace('\\', '/').Trim();
        }

        /// <inheritdoc />
        public string ResolvePath(string path)
        {
            return Path.GetFullPath(path);
        }
    }
}
