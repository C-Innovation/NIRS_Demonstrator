using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AvaloniaMvvmApp.Core
{
    /// <summary>
    /// Чтение и запись объектов в XML.
    /// <para>
    /// Сериализаторы кэшируются: <see cref="XmlSerializer"/> генерирует сборку
    /// при первом создании, поэтому пересоздавать его на каждый вызов дорого.
    /// </para>
    /// </summary>
    public static class XmlSerialization
    {
        private static readonly ConcurrentDictionary<Type, XmlSerializer> _serializerCache
            = new ConcurrentDictionary<Type, XmlSerializer>();

        private static readonly ConcurrentDictionary<(Type, string), XmlSerializer> _listSerializerCache
            = new ConcurrentDictionary<(Type, string), XmlSerializer>();

        /// <summary>
        /// Записывает объект в XML-файл.
        /// </summary>
        /// <returns><c>true</c>, если запись удалась.</returns>
        public static bool Serialize<T>(string path, T entity)
        {
            if (entity == null || string.IsNullOrEmpty(path))
                return false;

            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                };

                var serializer = GetSerializer(typeof(T));

                using var stream = new FileStream(path, FileMode.Create);
                using var writer = XmlWriter.Create(stream, settings);

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                serializer.Serialize(writer, entity, namespaces);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Читает объект из XML-файла. Возвращает значение по умолчанию,
        /// если файла нет либо он повреждён.
        /// </summary>
        public static T? Deserialize<T>(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return default;

            try
            {
                var serializer = GetSerializer(typeof(T));

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                return (T?)serializer.Deserialize(stream);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>
        /// Записывает список объектов в XML-файл.
        /// </summary>
        /// <param name="rootName">
        /// Имя корневого элемента. Пустое значение — <c>{ИмяТипа}List</c>.
        /// </param>
        public static bool SerializeList<T>(string path, List<T> entity, string rootName = "")
        {
            if (entity == null || string.IsNullOrEmpty(path))
                return false;

            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true,
                };

                var serializer = GetListSerializer<T>(rootName);

                using var stream = new FileStream(path, FileMode.Create);
                using var writer = XmlWriter.Create(stream, settings);

                var namespaces = new XmlSerializerNamespaces();
                namespaces.Add(string.Empty, string.Empty);
                serializer.Serialize(writer, entity, namespaces);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Читает список объектов из XML-файла.
        /// </summary>
        public static List<T>? DeserializeList<T>(string path, string rootName = "")
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                var serializer = GetListSerializer<T>(rootName);

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                return (List<T>?)serializer.Deserialize(stream);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static XmlSerializer GetSerializer(Type type)
        {
            return _serializerCache.GetOrAdd(type, t =>
            {
                var importer = new XmlReflectionImporter();
                var mapping = importer.ImportTypeMapping(t, null, null);
                return new XmlSerializer(mapping);
            });
        }

        private static XmlSerializer GetListSerializer<T>(string rootName)
        {
            string rootElement = string.IsNullOrEmpty(rootName)
                ? typeof(T).Name + "List"
                : rootName;

            return _listSerializerCache.GetOrAdd((typeof(T), rootElement), key =>
            {
                var attributes = new XmlAttributes
                {
                    XmlRoot = new XmlRootAttribute { ElementName = key.Item2 },
                };

                var overrides = new XmlAttributeOverrides();
                overrides.Add(typeof(List<T>), attributes);

                return new XmlSerializer(typeof(List<T>), overrides);
            });
        }
    }
}
