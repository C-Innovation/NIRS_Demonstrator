using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NIRS_Demonstrator
{
    public class ButterworthFilter
    {
        private readonly double[] _b;
        private readonly double[] _a;
        private readonly double[] _xHistory;
        private readonly double[] _yHistory;
        private readonly int _order;

        public ButterworthFilter(double cutoffFreq, double sampleRate, int order = 2)
        {
            _order = order;
            double normalizedFreq = cutoffFreq / (sampleRate / 2.0);

            CalculateButterworthCoefficients(normalizedFreq, order, out _b, out _a);

            _xHistory = new double[order + 1];
            _yHistory = new double[order + 1];
        }

        private void CalculateButterworthCoefficients(double Wn, int order, out double[] b, out double[] a)
        {
            if (order == 2)
            {
                double K = Math.Tan(Math.PI * Wn);
                double norm = 1.0 / (1.0 + Math.Sqrt(2.0) * K + K * K);

                b = new double[] { norm * K * K, 2.0 * norm * K * K, norm * K * K };
                a = new double[] { 1.0, 2.0 * norm * (K * K - 1.0), norm * (1.0 - Math.Sqrt(2.0) * K + K * K) };
            }
            else
            {
                throw new NotImplementedException("Поддерживается только порядок 2");
            }
        }

        public double Process(double input)
        {
            if (double.IsNaN(input) || double.IsInfinity(input))
                return 0;

            for (int i = _order; i > 0; i--)
            {
                _xHistory[i] = _xHistory[i - 1];
                _yHistory[i] = _yHistory[i - 1];
            }

            _xHistory[0] = input;

            double output = _b[0] * _xHistory[0] + _b[1] * _xHistory[1] + _b[2] * _xHistory[2]
                          - _a[1] * _yHistory[1] - _a[2] * _yHistory[2];

            if (double.IsNaN(output) || double.IsInfinity(output))
                output = 0;

            _yHistory[0] = output;
            return output;
        }

        public void Reset()
        {
            Array.Clear(_xHistory, 0, _xHistory.Length);
            Array.Clear(_yHistory, 0, _yHistory.Length);
        }
    }

    public class DynamicNormalizer
    {
        private double _min = double.MaxValue;
        private double _max = double.MinValue;
        private readonly Queue<double> _window;
        private readonly int _windowSize;
        private int _sampleCount = 0;

        public DynamicNormalizer(int windowSize = 1000)
        {
            _windowSize = windowSize;
            _window = new Queue<double>(windowSize);
        }

        public double Normalize(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.5;

            _sampleCount++;

            if (_window.Count >= _windowSize)
                _window.Dequeue();
            _window.Enqueue(value);

            // Пересчет min/max только когда окно достаточно заполнено
            if (_sampleCount >= _windowSize / 10) // Начинаем нормализацию после 10% заполнения
            {
                _min = _window.Min();
                _max = _window.Max();
            }

            double range = _max - _min;

            // Если диапазон слишком мал, возвращаем нормализованное значение относительно начального
            if (range < 1e-6)
            {
                // Используем фиксированный диапазон для начальных значений
                return 0.5;
            }

            double normalized = (value - _min) / range;
            return Math.Max(0.0, Math.Min(1.0, normalized));
        }

        public void Reset()
        {
            _window.Clear();
            _min = double.MaxValue;
            _max = double.MinValue;
            _sampleCount = 0;
        }
    }

    public class NIRSProcessor
    {
        private static readonly double[] TiaGains = { 1.0, 5.1, 20.0, 50.0 };
        private const int NumDistances = 4;
        private const int NumWavelengths = 2;
        private const int TotalChannels = 8;

        private readonly double _spatialSubtractionFactor;
        private readonly double[] _baseline;
        private readonly bool[] _baselineInitialized;
        private readonly ButterworthFilter _filter;
        private readonly DynamicNormalizer _normalizer;
        private readonly double _baselineAlpha;

        // Для отладки
        public double LastRawSignal { get; private set; }
        public double LastFilteredSignal { get; private set; }
        public double LastMuscleOxygenation { get; private set; }

        public NIRSProcessor(double sampleRate = 1000.0,
                            double cutoffFreq = 0.5,
                            double spatialFactor = 0.8,
                            double baselineAlpha = 0.001)
        {
            _spatialSubtractionFactor = spatialFactor;
            _baselineAlpha = baselineAlpha;

            _baseline = new double[TotalChannels];
            _baselineInitialized = new bool[TotalChannels];

            _filter = new ButterworthFilter(cutoffFreq, sampleRate, 2);
            _normalizer = new DynamicNormalizer(windowSize: (int)(sampleRate * 2));
        }

        public double ProcessSample(double[] rawVoltages)
        {
            if (rawVoltages.Length < TotalChannels)
                throw new ArgumentException($"Ожидается минимум {TotalChannels} каналов, получено {rawVoltages.Length}");

            // Шаг 1: Нормализация и расчет Delta OD
            double[] deltaOd = new double[TotalChannels];

            for (int i = 0; i < TotalChannels; i++)
            {
                int distIndex = i % NumDistances;
                double normalizedCurrent = rawVoltages[i] / TiaGains[distIndex];

                // Защита от некорректных значений
                if (normalizedCurrent <= 0 || double.IsNaN(normalizedCurrent))
                {
                    normalizedCurrent = 1e-10;
                }

                if (!_baselineInitialized[i])
                {
                    _baseline[i] = normalizedCurrent;
                    _baselineInitialized[i] = true;
                    deltaOd[i] = 0;
                }
                else
                {
                    // Расчет Delta OD: ln(V_baseline / V_current)
                    double ratio = _baseline[i] / normalizedCurrent;

                    if (ratio <= 0 || double.IsNaN(ratio) || double.IsInfinity(ratio))
                    {
                        deltaOd[i] = 0;
                    }
                    else
                    {
                        deltaOd[i] = Math.Log(ratio);
                    }

                    // Адаптивное обновление базовой линии
                    _baseline[i] = _baseline[i] * (1.0 - _baselineAlpha) +
                                  normalizedCurrent * _baselineAlpha;
                }
            }

            // Шаг 2: Пространственное вычитание
            // Индексы: 0,2,4,6 - 740нм, 1,3,5,7 - 850нм
            // Расстояния: 0,1 - 5.5мм, 2,3 - 10мм, 4,5 - 26.5мм, 6,7 - 32мм

            double shallow740 = (deltaOd[0] + deltaOd[2]) * 0.5;
            double shallow850 = (deltaOd[1] + deltaOd[3]) * 0.5;
            double deep740 = (deltaOd[4] + deltaOd[6]) * 0.5;
            double deep850 = (deltaOd[5] + deltaOd[7]) * 0.5;

            // Шаг 3: Спектральное вычитание
            double superficialSignal = shallow740 - shallow850;
            double deepSignal = deep740 - deep850;
            double muscleOxygenation = deepSignal - _spatialSubtractionFactor * superficialSignal;

            LastMuscleOxygenation = muscleOxygenation;

            // Шаг 4: Фильтрация
            double filteredSignal = _filter.Process(muscleOxygenation);
            LastFilteredSignal = filteredSignal;

            // Шаг 5: Инверсия (при сокращении оксигенация падает)
            double contractionSignal = -filteredSignal;
            LastRawSignal = contractionSignal;

            // Шаг 6: Нормализация
            double normalizedOutput = _normalizer.Normalize(contractionSignal);

            return normalizedOutput;
        }

        public double[] ProcessFile(string inputPath)
        {
            var results = new List<double>();
            var debugInfo = new List<string>();

            var lines = File.ReadAllLines(inputPath);

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var line = lines[lineIdx];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Парсинг CSV с европейским форматом
                var parts = line.Split(';');

                // Берем только первые 8 значений (игнорируем последнее, если оно есть)
                if (parts.Length < TotalChannels)
                    continue;

                double[] voltages = new double[TotalChannels];
                bool parseError = false;

                for (int i = 0; i < TotalChannels; i++)
                {
                    // Замена запятой на точку для парсинга
                    string valueStr = parts[i].Replace(',', '.').Trim();
                    if (!double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out voltages[i]))
                    {
                        parseError = true;
                        break;
                    }
                }

                if (parseError)
                    continue;

                double result = ProcessSample(voltages);
                results.Add(result);

                // Отладочная информация для первых 100 строк
                if (lineIdx < 100)
                {
                    debugInfo.Add($"Line {lineIdx}: " +
                        $"V=[{string.Join(",", voltages.Select(v => v.ToString("F3")))}] " +
                        $"DeltaOD=[{string.Join(",", new double[8].Select((_, i) => {
                            int dist = i % 4;
                            double norm = voltages[i] / TiaGains[dist];
                            if (!_baselineInitialized[i]) return 0;
                            return Math.Log(_baseline[i] / norm);
                        }).Select(v => v.ToString("F4")))}] " +
                        $"MuscleO2={LastMuscleOxygenation:F6} " +
                        $"Filtered={LastFilteredSignal:F6} " +
                        $"Output={result:F6}");
                }
            }

            // Запись отладочной информации
            File.WriteAllLines("debug_nirs.txt", debugInfo);

            return results.ToArray();
        }

        public void Reset()
        {
            for (int i = 0; i < TotalChannels; i++)
            {
                _baselineInitialized[i] = false;
            }
            _filter?.Reset();
            _normalizer?.Reset();
        }
    }
}
