using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class HorizontalGrid : Line
    {
        #region Styled Properties



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
        public HorizontalGrid()
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
        public HorizontalGrid(AvaloniaList<double> dashArray, double strokeThickness, IBrush stroke, double opacity)
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

    public static class HorizontalGridExtended
    {
        public static void UpdatePositions(this List<HorizontalGrid> horizontalGrids, Size chartAreaSize)
        { 

            double delta = chartAreaSize.Height / (double)(horizontalGrids.Count + 1);
            double current = delta;
            foreach (HorizontalGrid horizontalGrid in horizontalGrids)
            {
                horizontalGrid.StartPoint = new Point(0, current);
                horizontalGrid.EndPoint = new Point(chartAreaSize.Width, current);
                current += delta;
            }
        }
    }
}
