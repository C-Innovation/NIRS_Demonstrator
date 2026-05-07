using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public static class XmlSerialization
    {

        #region Protected Members

        #endregion

        #region Private Members

        /// <summary>
        /// Словарь ранее уже сереализованных значений.
        /// </summary>
        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializerCache = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<Type, Dictionary<string, XmlSerializer>> _xmlSerializerListCache = new Dictionary<Type, Dictionary<string, XmlSerializer>>();

        #endregion

        #region Public Properties

        #endregion

        #region Public Events

        #endregion

        #region Private Callbacks

        #endregion

        #region Public Methods

        /// <summary>
        /// Сереализация объекта в xml формат.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="path">Путь, по которому сохраняется xml файл.</param>
        /// <param name="entity">Объект сереализации.</param>
        /// <returns>Флаг с результатом выполнения функции.</returns>
        public static bool Serialize<T>(string path, T entity)
        {
            if (entity == null || string.IsNullOrEmpty(path))
            {
                return false;
            }

            var result = true;

            try
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    Indent = true
                };

                var formatter = CreateDefaultXmlSerializer(typeof(T));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    {
                        var xmlns = new XmlSerializerNamespaces();

                        xmlns.Add(string.Empty, string.Empty);
                        formatter.Serialize(writer, entity, xmlns);
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Сереализация списка объектов в xml формат.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="path">Путь, по которому сохраняется xml файл.</param>
        /// <param name="entity">Объект сереализации.</param>
        /// <param name="rootName">Имя корневого элемента или пустое значение</param>
        /// <returns>Флаг с результатом выполнения функции</returns>
        public static bool SerializeList<T>(string path, List<T> entity, string rootName = "")
        {
            if (entity == null || string.IsNullOrEmpty(path))
                return false;
            var result = true;
            try
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    Indent = true
                };

                var formatter = CreateXmlSerializerList<T>(rootName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    using (XmlWriter writer = XmlWriter.Create(stream, settings))
                    {
                        var xmlns = new XmlSerializerNamespaces();

                        xmlns.Add(string.Empty, string.Empty);
                        formatter.Serialize(writer, entity, xmlns);
                    }
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Десереализация объекта из xml формата.
        /// </summary>
        /// <typeparam name="T">Тип объекта.</typeparam>
        /// <param name="path">Путь к xml файлу.</param>
        /// <returns>Объект сереализации.</returns>
        public static T Deserialize<T>(string path)
        {
            T result = default;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return result;
            }

            try
            {
                var formatter = CreateDefaultXmlSerializer(typeof(T));
                using (var stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    result = (T)formatter.Deserialize(stream);
                }
            }
            catch
            {
                result = default;
            }

            return result;
        }
        /// <summary>
        /// Десереализация списка объектов из xml формата.
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="path">Путь к xml файлу</param>
        /// <param name="rootName">Имя корневого элемента или пустое значение</param>
        /// <returns>Список объектов сереализации</returns>
        public static List<T> DeserializeList<T>(string path, string rootName = "")
        {
            List<T> result = default;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return result;
            }
            try
            {
                var formatter = CreateXmlSerializerList<T>(rootName);
                using (var stream = new FileStream(path, FileMode.OpenOrCreate))
                {
                    result = (List<T>)formatter.Deserialize(stream);
                }
            }
            catch
            {
                result = default;
            }

            return result;
        }

        #endregion

        #region Private Methods

        
        /// <summary>
        /// Создание сереализатора.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>Сереализатор.</returns>
        private static XmlSerializer CreateDefaultXmlSerializer(Type type)
        {
            if (_xmlSerializerCache.TryGetValue(type, out var serializer))
            {
                return serializer;
            }
            else
            {
                var importer = new XmlReflectionImporter();
                var mapping = importer.ImportTypeMapping(type, null, null);

                serializer = new XmlSerializer(mapping);
                return _xmlSerializerCache[type] = serializer;
            }
        }

        private static XmlSerializer CreateXmlSerializerList<T>(string rootName)
        {
            var rootElement = string.IsNullOrEmpty(rootName) ? $"{typeof(T).Name.Replace("Map", "")}List" : rootName;
            var t = typeof(T);
            if (_xmlSerializerListCache.TryGetValue(t, out var sDic) && sDic.TryGetValue(rootElement, out var serializer))
            {
                return serializer;
            }
            // Create an XmlAttributes to override the default root element.
            XmlAttributes xmlAttributes = new XmlAttributes();
            // Create an XmlRootAttribute and set its element name and namespace.
            XmlRootAttribute xmlRootAttribute = new XmlRootAttribute
            {
                ElementName = rootElement
            };
            // Set the XmlRoot property to the XmlRoot object.
            xmlAttributes.XmlRoot = xmlRootAttribute;
            XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
            xmlAttributeOverrides.Add(typeof(List<T>), xmlAttributes);
            // Create the Serializer, and return it.
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(List<T>), xmlAttributeOverrides);
            if (!_xmlSerializerListCache.TryGetValue(t, out sDic))
            {
                sDic = _xmlSerializerListCache[t] = new Dictionary<string, XmlSerializer>();
            }
            return sDic[rootElement] = xmlSerializer;
        }

        #endregion
    }
}
