using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class VerticalGrid : Line
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        #endregion

        #region Public Properties

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public VerticalGrid()
        {
            this.StrokeDashArray = new AvaloniaList<double> { 2.0, 2.0 };
            this.StrokeThickness = 2;
            this.Stroke = Brushes.DarkGray;
            this.Opacity = 0.8;
        }

        /// <summary>
        /// Extended constructor
        /// </summary>
        /// <param name="dashArray">Grid <see cref="Line"/> dash array</param>
        /// <param name="strokeThickness">Grid <see cref="Line"/> thickness</param>
        /// <param name="stroke">Grid <see cref="Line"/> color</param>
        /// <param name="opacity">Grid <see cref="Line"/> opacity</param>
        public VerticalGrid(AvaloniaList<double> dashArray, double strokeThickness, IBrush stroke, double opacity)
        {
            this.StrokeDashArray = dashArray;
            this.StrokeThickness = strokeThickness;
            this.Stroke = stroke;
            this.Opacity = opacity;
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion
    }

    public static class VerticalGridExtended
    {
        public static void UpdatePositions(this List<VerticalGrid> verticalGrids, Size chartAreaSize)
        {

            double delta = chartAreaSize.Width / (double)(verticalGrids.Count + 1);
            double current = delta;
            foreach (VerticalGrid verticalGrid in verticalGrids)
            {
                verticalGrid.StartPoint = new Point(current, 0);
                verticalGrid.EndPoint = new Point(current, chartAreaSize.Height);
                current += delta;
            }
        }
    }
}
