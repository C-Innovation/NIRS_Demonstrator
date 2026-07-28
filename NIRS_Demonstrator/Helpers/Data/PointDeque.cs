using Avalonia;
using System;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Растущая двусторонняя очередь точек поверх кольцевого массива.
    /// <para>
    /// Заменяет <see cref="System.Collections.Generic.List{T}"/> в качестве хранилища
    /// точек серии: удаление точки, вышедшей за левую границу окна, стоит O(1)
    /// вместо O(n) (List.RemoveAt(0) сдвигает весь массив, что на потоке отсчётов
    /// вырождается в O(n²) за сессию).
    /// </para>
    /// Точки всегда добавляются в конец и всегда упорядочены по X по возрастанию,
    /// что позволяет искать границы окна бинарным поиском.
    /// </summary>
    public sealed class PointDeque
    {
        private Point[] _items;
        private int _head;
        private int _count;

        public PointDeque(int capacity = 1024)
        {
            if (capacity < 1)
                capacity = 1;
            _items = new Point[capacity];
        }

        public int Count => _count;

        public Point this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return _items[Wrap(_head + index)];
            }
        }

        public Point First => this[0];

        public Point Last => this[_count - 1];

        public void AddLast(Point point)
        {
            if (_count == _items.Length)
                Grow();

            _items[Wrap(_head + _count)] = point;
            _count++;
        }

        /// <summary>
        /// Удаляет первую точку за O(1).
        /// </summary>
        public void RemoveFirst()
        {
            if (_count == 0)
                return;

            _head = Wrap(_head + 1);
            _count--;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Индекс первой точки с X больше либо равным <paramref name="x"/>.
        /// Возвращает <see cref="Count"/>, если таких точек нет.
        /// </summary>
        public int LowerBound(double x)
        {
            int lo = 0;
            int hi = _count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (_items[Wrap(_head + mid)].X < x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        /// <summary>
        /// Индекс первой точки с X строго больше <paramref name="x"/>.
        /// Возвращает <see cref="Count"/>, если таких точек нет.
        /// </summary>
        public int UpperBound(double x)
        {
            int lo = 0;
            int hi = _count;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                if (_items[Wrap(_head + mid)].X <= x)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Wrap(int index)
        {
            int length = _items.Length;
            return index >= length ? index - length : index;
        }

        private void Grow()
        {
            Point[] bigger = new Point[_items.Length * 2];

            int firstPart = Math.Min(_count, _items.Length - _head);
            Array.Copy(_items, _head, bigger, 0, firstPart);
            if (firstPart < _count)
                Array.Copy(_items, 0, bigger, firstPart, _count - firstPart);

            _items = bigger;
            _head = 0;
        }
    }
}
