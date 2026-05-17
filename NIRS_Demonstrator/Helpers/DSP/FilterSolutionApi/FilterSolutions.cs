using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class FilterSolutions
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        private double _K;

        private List<FilterSolutionsData> Cascades;

        private List<double[]> _historyX;
        private List<double[]> _historyY;
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
        public FilterSolutions(string datPath)
        {
            if (string.IsNullOrEmpty(datPath))
                throw new ArgumentException("Path can`t be empty!", "datPath");

            if(!File.Exists(datPath))
                throw new ArgumentException("FilterSolution!", "datPath");

            _historyX = new List<double[]>();
            _historyY = new List<double[]>();

            Cascades = new List<FilterSolutionsData>();

            ParseDatFile(datPath);

            foreach (var cascade in Cascades)
            {
                
                int order = cascade.Denominators.Count - 1;
                _historyX.Add(new double[order]);
                _historyY.Add(new double[order]);
            }
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods

        //public double Process(double val)
        //{
        //    double res = _K;
        //    foreach(FilterSolutionsData s in Cascades)
        //    {
        //        res += (CalcPolynomialVal(val, s.Numerators) / CalcPolynomialVal(val, s.Denominators));
        //    }
        //    return res;
        //}

        public double Process(double val)
        {
            // 1. Вклад константы K (прямая связь)
            double totalResult = _K * val;

            // 2. Обработка каждого каскада (Term)
            for (int i = 0; i < Cascades.Count; i++)
            {
                var cascade = Cascades[i];
                double[] hx = _historyX[i]; // История входов для этого каскада
                double[] hy = _historyY[i]; // История выходов для этого каскада

                // --- Расчет текущего выхода y[n] ---
                // Формула: y[n] = (b0*x[n] + b1*x[n-1]...) - (a1*y[n-1] + a2*y[n-2]...)

                double currentY = 0;

                // Слагаемые числителя (B coeffs)
                // val - это текущий x[n]
                if (cascade.Numerators.Count > 0) currentY += cascade.Numerators[0] * val;
                for (int j = 1; j < cascade.Numerators.Count; j++)
                {
                    // hx[j-1] содержит x[n-j]
                    if (j - 1 < hx.Length)
                        currentY += cascade.Numerators[j] * hx[j - 1];
                }

                // Слагаемые знаменателя (A coeffs) - с обратным знаком (переносим в правую часть уравнения)
                // Знаменатель в файле обычно начинается с 1, поэтому начинаем с индекса 1 (a1)
                for (int j = 1; j < cascade.Denominators.Count; j++)
                {
                    // hy[j-1] содержит y[n-j]
                    if (j - 1 < hy.Length)
                        currentY -= cascade.Denominators[j] * hy[j - 1];
                }

                // --- Обновление истории (сдвиг регистров задержки) ---
                // Сдвигаем старые значения вправо (x[n-1] -> x[n-2])
                for (int k = hx.Length - 1; k > 0; k--)
                {
                    hx[k] = hx[k - 1];
                    hy[k] = hy[k - 1];
                }
                // Записываем новые текущие значения в начало (индекс 0)
                if (hx.Length > 0) hx[0] = val;
                if (hy.Length > 0) hy[0] = currentY;

                // Суммируем выход каскада к общему результату
                totalResult += currentY;
            }

            return totalResult;
        }

        #endregion

        #region Private Methods

        /*
            K = 4.78e-05

            Term 1:
            1.25
            1, -.751

            Term 2:
            -1.35, 1.13
            1, -1.53, .593

            Term 3:
            .101, -.169
            1, -1.61, .7
         **/

        private bool ParseDatFile(string datPath)
        {
            string[] lines = File.ReadAllLines(datPath);
            string strK = lines[0].Substring(lines[0].LastIndexOf(" ") + 1, lines[0].Length - lines[0].LastIndexOf(" ") - 1);
            if(! double.TryParse(strK, CultureInfo.InvariantCulture, out _K))
                return false;

            for (int i = 1; i < lines.Length; i += 4)
            {
                if (!lines[i + 1].Contains("Term"))
                    return false;

                FilterSolutionsData data = new FilterSolutionsData();

                data.Numerators = ParseCoefsLine(lines[i + 2]);
                if(data.Numerators == null) 
                    return false;

                data.Denominators = ParseCoefsLine(lines[i + 3]);
                if (data.Numerators == null) 
                    return false;

                Cascades.Add(data);
            }

            return true;
        }


        private List<double> ParseCoefsLine(string line)
        {
            List<double> coefs = new List<double>();
            while (line.Length > 0)
            {
                double tmpD = 0;
                int index = (line.IndexOf(",") > 0) ? line.IndexOf(",") : line.Length;
                if (!double.TryParse(line.Substring(0, index), CultureInfo.InvariantCulture, out tmpD))
                    return null;
                coefs.Add(tmpD);
                line = line.Substring(index == line.Length ? index : index + 2);
            }

            return coefs;
        }

        /// <summary>
        /// Calculate polynomial value
        /// </summary>
        /// <param name="val"></param>
        /// <param name="polynome"></param>
        /// <returns></returns>
        private double CalcPolynomialVal(double val, List<double> polynomial)
        {
            int pow = polynomial.Count - 1;
            double res = 0;
            for (int i = 0; i < polynomial.Count; i++) 
            {
                res += polynomial[i] * Math.Pow(val, pow);
                pow--;
            }
            return res;
        }

        #endregion
    }

    public class FilterSolutionsData
    {
        public List<double> Numerators;
        public List<double> Denominators;

        public FilterSolutionsData()
        {
            Numerators= new List<double>();
            Denominators= new List<double>();
        }
    }
}
