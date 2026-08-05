using System;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Диапазон выделения по горизонтальной оси в реальных значениях.
    /// </summary>
    public class ChartSelectionRangeEventArgs : EventArgs
    {
        #region Public Properties

        public double StartValue { get; }

        public double StopValue { get; }

        /// <summary>
        /// Ширина выделения в реальных значениях оси X.
        /// </summary>
        public double Width => StopValue - StartValue;

        #endregion

        #region Constructor

        public ChartSelectionRangeEventArgs(double startValue, double stopValue)
        {
            if (startValue > stopValue)
                (startValue, stopValue) = (stopValue, startValue);

            StartValue = startValue;
            StopValue = stopValue;
        }

        #endregion
    }
}
