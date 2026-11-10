using System;
using System.IO;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Хранилище настроек в XML-файле.
    /// </summary>
    public class XmlSettingsStore<TSettings> : ISettingsStore<TSettings>
        where TSettings : class, new()
    {
        private readonly string _path;

        /// <param name="path">Полный путь к файлу настроек.</param>
        public XmlSettingsStore(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Путь к файлу настроек не задан.", nameof(path));

            _path = path;
        }

        /// <inheritdoc />
        public TSettings? Read()
        {
            if (!File.Exists(_path))
                return null;

            return XmlSerialization.Deserialize<TSettings>(_path);
        }

        /// <inheritdoc />
        public bool Write(TSettings settings)
        {
            if (settings == null)
                return false;

            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return XmlSerialization.Serialize(_path, settings);
        }
    }
}
