using Avalonia;
using System;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Одна выделенная по горизонтали область: диапазон по оси X (по всей высоте
    /// оси Y) и попавшие в него данные исследования.
    /// <para>
    /// Ось X у графиков просмотра — это номер строки в NirsData (точки серий
    /// добавляются с X = индекс строки), поэтому область хранится как диапазон
    /// строк <see cref="StartIndex"/>..<see cref="StopIndex"/> включительно,
    /// а значения берутся прямо из колонок NirsData — то есть в реальных
    /// величинах, а не в координатах области построения.
    /// </para>
    /// <para>
    /// Колонок в csv девять: по четыре на каждый график и девятая — общая для
    /// обоих. Серии Ai в csv не сохраняются, считаются приложением и в область
    /// не попадают.
    /// </para>
    /// </summary>
    public class ChartSelectionArea
    {
        #region Public Constants

        /// <summary>
        /// Индекс общей для обоих графиков колонки (Nirs1Series740_TotalVal /
        /// Nirs1Series850_TotalVal) — той, что редактируется и сохраняется.
        /// </summary>
        public const int TotalValueColumn = 8;

        #endregion

        #region Private Members

        private readonly List<List<Point>> _ColumnPoints = new List<List<Point>>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Начало выделения в реальных значениях оси X.
        /// </summary>
        public double StartValue { get; }

        /// <summary>
        /// Конец выделения в реальных значениях оси X.
        /// </summary>
        public double StopValue { get; }

        /// <summary>
        /// Первая строка NirsData, попавшая в выделение (включительно).
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// Последняя строка NirsData, попавшая в выделение (включительно).
        /// </summary>
        public int StopIndex { get; private set; }

        /// <summary>
        /// Количество строк NirsData в выделении.
        /// </summary>
        public int Count => StopIndex >= StartIndex ? StopIndex - StartIndex + 1 : 0;

        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Точки выделения: внешний список — колонки csv в их исходном порядке,
        /// внутренний — точки колонки, где X — индекс строки NirsData,
        /// а Y — реальное значение.
        /// </summary>
        public List<List<Point>> ColumnPoints => _ColumnPoints;

        /// <summary>
        /// Количество колонок, попавших в область.
        /// </summary>
        public int ColumnCount => _ColumnPoints.Count;

        /// <summary>
        /// Точки общей колонки — той, что редактируется и сохраняется.
        /// </summary>
        public List<Point> TotalValuePoints => GetColumnPoints(TotalValueColumn);

        #endregion

        #region Constructor

        public ChartSelectionArea(double startValue, double stopValue, IReadOnlyList<List<double>> nirsData)
        {
            if (startValue > stopValue)
                (startValue, stopValue) = (stopValue, startValue);

            StartValue = startValue;
            StopValue = stopValue;

            Rebuild(nirsData);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Пересобирает значения области из NirsData. Нужно вызывать после
        /// редактирования данных, иначе область останется со старыми значениями.
        /// Списки точек обновляются на месте, поэтому ссылки на них остаются
        /// действительными.
        /// </summary>
        public void Rebuild(IReadOnlyList<List<double>> nirsData)
        {
            int columnCount = nirsData != null ? nirsData.Count : 0;
            int rowCount = columnCount > 0 ? nirsData[0].Count : 0;

            StartIndex = (int)Math.Ceiling(StartValue);
            StopIndex = (int)Math.Floor(StopValue);

            if (StartIndex < 0)
                StartIndex = 0;

            if (StopIndex > rowCount - 1)
                StopIndex = rowCount - 1;

            while (_ColumnPoints.Count < columnCount)
                _ColumnPoints.Add(new List<Point>());

            while (_ColumnPoints.Count > columnCount)
                _ColumnPoints.RemoveAt(_ColumnPoints.Count - 1);

            for (int column = 0; column < columnCount; column++)
            {
                List<Point> points = _ColumnPoints[column];
                List<double> values = nirsData[column];

                points.Clear();

                for (int row = StartIndex; row <= StopIndex && row < values.Count; row++)
                    points.Add(new Point(row, values[row]));
            }
        }

        /// <summary>
        /// Точки одной колонки csv. Для отсутствующей колонки — пустой список.
        /// </summary>
        public List<Point> GetColumnPoints(int column)
        {
            if ((uint)column >= (uint)_ColumnPoints.Count)
                return new List<Point>();

            return _ColumnPoints[column];
        }

        /// <summary>
        /// Попадает ли строка NirsData в выделение.
        /// </summary>
        public bool ContainsRow(int rowIndex)
        {
            return !IsEmpty && rowIndex >= StartIndex && rowIndex <= StopIndex;
        }

        #endregion
    }
}
