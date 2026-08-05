using Avalonia;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Снимок преобразования «реальные значения &lt;-&gt; точки на области построения»
    /// для одной серии в момент времени.
    /// <para>
    /// Прямое преобразование (<see cref="ToCanvas"/>) — это то, что делает
    /// <see cref="Series"/> при каждой перерисовке. Обратное (<see cref="ToValue"/>)
    /// нужно, чтобы из экранных точек (например, попавших в выделенную область)
    /// получить обратно реальные значения.
    /// </para>
    /// Преобразование зависит от текущего окна просмотра и размера области построения,
    /// поэтому его снимок сохраняется вместе с выделением: после прокрутки или
    /// изменения размера окна текущее преобразование уже не совпадает с тем,
    /// при котором точки были захвачены.
    /// </summary>
    public readonly struct SeriesViewTransform
    {
        #region Public Properties

        /// <summary>
        /// Реальное значение X у левой границы области построения.
        /// </summary>
        public readonly double ValueLeft;

        /// <summary>
        /// Реальное значение Y у нижней границы области построения.
        /// </summary>
        public readonly double ValueBottom;

        /// <summary>
        /// Реальное значение Y у верхней границы области построения.
        /// </summary>
        public readonly double ValueTop;

        public readonly double AxisSizeX;
        public readonly double AxisSizeY;
        public readonly double AreaWidth;
        public readonly double AreaHeight;

        /// <summary>
        /// Преобразование пригодно к использованию: область построения уже
        /// имеет размер, а оси — ненулевой масштаб.
        /// </summary>
        public bool IsValid => AxisSizeX > 0 && AxisSizeY > 0 && AreaWidth > 0 && AreaHeight > 0;

        #endregion

        #region Constructor

        public SeriesViewTransform(
            double valueLeft, double axisSizeX, double areaWidth,
            double valueBottom, double valueTop, double axisSizeY, double areaHeight)
        {
            ValueLeft = valueLeft;
            AxisSizeX = axisSizeX;
            AreaWidth = areaWidth;
            ValueBottom = valueBottom;
            ValueTop = valueTop;
            AxisSizeY = axisSizeY;
            AreaHeight = areaHeight;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Реальное значение -> точка на области построения.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ToCanvas(Point value)
        {
            double x = ((value.X - ValueLeft) / AxisSizeX) * AreaWidth;
            double y;

            // Значения за пределами окна прижимаются к его границам — так же,
            // как это делает отрисовка серии.
            if (value.Y <= ValueBottom)
                y = AreaHeight;
            else if (value.Y >= ValueTop)
                y = 0;
            else
                y = AreaHeight - (((value.Y - ValueBottom) / AxisSizeY) * AreaHeight);

            return new Point(x, y);
        }

        /// <summary>
        /// Точка на области построения -> реальное значение.
        /// <para>
        /// Обратное преобразование точно только для точек, которые при отрисовке
        /// не были прижаты к верхней или нижней границе области: для прижатых
        /// вернётся значение соответствующей границы, а не исходное.
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point ToValue(Point canvasPoint)
        {
            return new Point(ToValueX(canvasPoint.X), ToValueY(canvasPoint.Y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToCanvasX(double valueX)
        {
            return ((valueX - ValueLeft) / AxisSizeX) * AreaWidth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToValueX(double canvasX)
        {
            return ValueLeft + ((canvasX / AreaWidth) * AxisSizeX);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToValueY(double canvasY)
        {
            return ValueBottom + (((AreaHeight - canvasY) / AreaHeight) * AxisSizeY);
        }

        #endregion
    }
}
