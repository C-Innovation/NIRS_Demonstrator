using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public static class CsvParser
    {
        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        #endregion

        #region Public Events

        #endregion

        #region Private Callbacks

        #endregion

        #region Public Methods
        /// <summary>
        /// Парсит CSV файл, возвращая список колонок, где каждая колонка - List<double>
        /// </summary>
        public static List<List<double>> ParseCsvColumns(string filePath)
        {
            var columns = new List<List<double>>();
            bool isInitialized = false;

            // Используем InvariantCulture, чтобы гарантировать, что точка всегда будет восприниматься как разделитель дроби
            var culture = CultureInfo.InvariantCulture;

            using (var reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Разделяем строку по ';'. 
                    // RemoveEmptyEntries игнорирует последнюю пустую ячейку из-за завершающей ';'
                    string[] values = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    // Инициализируем списки для колонок при чтении первой строки
                    if (!isInitialized)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            columns.Add(new List<double>());
                        }
                        isInitialized = true;
                    }

                    // Заполняем колонки значениями
                    // Проверка i < columns.Count защищает от ошибок, если в файле встретится строка с меньшим числом значений
                    for (int i = 0; i < values.Length && i < columns.Count; i++)
                    {
                        string rawValue = values[i].Trim();

                        // Заменяем запятую на точку для корректного парсинга в double
                        string normalizedValue = rawValue.Replace(',', '.');

                        if (double.TryParse(normalizedValue, NumberStyles.Any, culture, out double result))
                        {
                            columns[i].Add(result);
                        }
                        else
                        {
                            // Если встретилось нечисловое значение, добавляем NaN (или можно добавить 0 / пропустить)
                            columns[i].Add(double.NaN);
                        }
                    }
                }
            }

            return columns;
        }
        #endregion

        #region Private Methods

        #endregion
    }
}
