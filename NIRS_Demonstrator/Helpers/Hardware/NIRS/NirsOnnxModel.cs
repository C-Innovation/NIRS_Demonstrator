// ============================================================================
//  NirsOnnxModel.cs — вызов обученной модели из C# через ONNX Runtime.
//
//  Установка:
//      dotnet add package Microsoft.ML.OnnxRuntime
//
//  Модель рекуррентная и имеет два входа и два выхода:
//      вход :  x (1,8)   h (1,16)
//      выход:  y (1,1)   h_new (1,16)
//  Скрытое состояние ONNX Runtime между вызовами не хранит — его держит
//  этот класс, ровно как nirs_nn_runtime.c на микроконтроллере.
//
//  Класс реализует ExampleDynamic.INirsModel, поэтому его можно сразу
//  передать в CompareWithModel() и сверить с детерминированным алгоритмом.
//
//  ВАЖНО: подготовка входа и прореживание здесь должны быть ТЕ ЖЕ, что при
//  обучении. Если меняете --decim в 02_train.py, поменяйте Decim и тут.
//
//  Признаки по умолчанию берёт ЯДРО (NirsContraction.NnFeatures) — так же,
//  как их готовит 01_build_dataset.py --core-features и как считает прошивка
//  через nirs_nn_features(). Ядро учитывает полярность каналов и слежение
//  базы за дрейфом, чего в старом предфильтре нет.
//
//  Для моделей, обученных ДО перехода на признаки ядра (без --core-features),
//  передайте useCoreFeatures: false — тогда вход готовит NirsPrefilter, как
//  раньше. Перепутать эти два режима — значит подать сети совсем не тот вход,
//  на котором она училась: выход становится почти константой и перестаёт
//  возвращаться в ноль.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace NIRS_Demonstrator
{
    public sealed class NirsOnnxModel : IDisposable
    {
        public const int NCh   = 8;
        public const int NState = 16;   // должно совпадать с --units при обучении
        public const int Decim  = 10;   // должно совпадать с --decim при обучении
        public const float WarmupS = 3.0f;  // до готовности выход не использовать

        readonly InferenceSession _sess;
        readonly string _inX, _inH, _outY, _outH;

        readonly bool _useCore;
        readonly NirsContraction _core;      // считается всегда (см. ниже)
        readonly NirsPrefilter   _pre;       // только при _useCore == false
        readonly float[] _feat = new float[NCh];
        readonly float[] _acc  = new float[NCh];
        readonly float[] _state = new float[NState];
        int _accN, _sub;
        long _n;
        readonly long _warmupN;
        float _out;

        /// <summary>Последнее предсказание, 0..1. Между вызовами сети удерживается.</summary>
        public float Out01 => _out;
        /// <summary>Пока false — выход не использовать: предфильтр ещё не нашёл
        /// уровень покоя, а состояние GRU не сошлось. На реальной записи это
        /// разница между ошибкой 0.100 (с учётом старта) и 0.061 (без него).</summary>
        public bool Ready => _n >= _warmupN;
        public float OutVolts(float vmax = 3.30f) => _out * vmax;

        /// <summary>Выход детерминированного ядра, 0..1. Ядро считается всегда,
        /// даже когда признаки для сети готовит предфильтр: оно стоит единицы
        /// микросекунд, а его флаги качества сигнала сети взять неоткуда.</summary>
        public float AlgoOut01 => _core.Out01;
        /// <summary>Флаги ядра (насыщение, нет каналов, стабильность и т. д.).</summary>
        public NirsFlags AlgoFlags => _core.Flags;

        // ------------------------------------------------------------------
        //  Валидность данных
        // ------------------------------------------------------------------
        /// <summary>
        /// РАБОЧИЙ признак: датчик на мышце, каналы набрали базу и знак, выход
        /// правильно повторяет мышцу. Поднимается за единицы секунд (измерено
        /// 1.5-10 с на записях S01-S04) и падает в ту же выборку, когда датчик
        /// сняли. Именно этим гейтить выход и индикацию.
        ///
        /// Отличие от <see cref="Ready"/>: Ready закрывает только прогрев
        /// (3 с, сходимость состояния GRU) и, раз поднявшись, больше не падает.
        /// Tracking отслеживается непрерывно.
        ///
        /// Выпад ОДНОГО канала (мигание на пределе шкалы ОУ, смена знака,
        /// короткий клип на сильном сокращении) флаг не роняет — он отнимает
        /// лишь долю этого канала от <see cref="Health"/>. Роняет его только
        /// потеря большей части каналов, то есть настоящее снятие датчика.
        /// </summary>
        public bool Tracking => Ready && _core.Tracking;

        /// <summary>
        /// МЕДЛЕННЫЙ признак: вдобавок к Tracking устоялся масштаб
        /// (автокалибровка максимума). Нужен там, где важна абсолютная
        /// величина: разметка датасета, пороги в долях от максимума.
        /// Измерено: 20-50 с, и раньше шкала просто неизвестна — это не
        /// лечится уменьшением константы.
        /// </summary>
        public bool Stable => Ready && _core.Stable;

        /// <summary>Здоровье установки, 0..1: доля веса работающих каналов
        /// относительно того, сколько их тут обычно работает. 1.0 — все на
        /// месте, ~0.8 — выпал один из пяти, 0 — датчик снят. Хорошая
        /// величина для индикатора качества контакта.</summary>
        public float Health => _core.Health;

        /// <summary>Приблизительный ход адаптации, 0..1 — для полоски
        /// прогресса. 0..0.5 — идёт поиск каналов, 0.5..1 — устаканивается
        /// масштаб.</summary>
        public float StableProgress => _core.StableProgress;

        /// <param name="useCoreFeatures">true (по умолчанию) — вход готовит ядро
        /// через NnFeatures(); так же, как 01_build_dataset.py --core-features.
        /// false — старый NirsPrefilter, для моделей, обученных до перехода.</param>
        public NirsOnnxModel(string onnxPath, float fs = 1000f, bool useCoreFeatures = true)
        {
            _sess = new InferenceSession(onnxPath);
            _useCore = useCoreFeatures;

            // Ядро поднимаем в обоих режимах: в режиме признаков ядра оно даёт
            // вход сети, а в режиме предфильтра — флаги качества сигнала и
            // признак стабилизации (Stable), которых у предфильтра нет.
            var cfg = new NirsConfig { Fs = fs };
            _core   = new NirsContraction(cfg);
            if (!_useCore) _pre = new NirsPrefilter(fs);
            _warmupN = (long)(WarmupS * fs);

            // Имена тензоров зависят от версии экспортёра, поэтому находим их
            // по размерности, а не по имени: 8 — признаки, 16 — состояние.
            var ins  = _sess.InputMetadata.ToList();
            var outs = _sess.OutputMetadata.ToList();

            _inX  = ins.First(p => Last(p.Value.Dimensions)  == NCh).Key;
            _inH  = ins.First(p => Last(p.Value.Dimensions)  == NState).Key;
            _outY = outs.First(p => Last(p.Value.Dimensions) == 1).Key;
            _outH = outs.First(p => Last(p.Value.Dimensions) == NState).Key;

            Console.WriteLine($"ONNX загружена: входы {_inX}(8), {_inH}({NState}); " +
                              $"выходы {_outY}(1), {_outH}({NState})");
            Reset();
        }

        static int Last(int[] dims) => dims.Length == 0 ? -1 : dims[dims.Length - 1];

        public void Reset()
        {
            _core.Reset();
            if (!_useCore) _pre.Reset();
            Array.Clear(_state, 0, _state.Length);
            Array.Clear(_acc, 0, _acc.Length);
            _accN = 0; _sub = 0; _out = 0f; _n = 0; NanEvents = 0;
        }

        /// <summary>Одна выборка: 8 напряжений -> уровень 0..1.
        /// Вызывать строго с частотой fs и строго по порядку.</summary>
        public float Predict(float[] volts)
        {
            _n++;
            // признаки готовятся на ПОЛНОЙ частоте: их постоянные времени
            // рассчитаны именно под неё
            _core.Update(volts);                       // всегда: флаги и Stable
            if (_useCore) _core.NnFeatures(_feat);
            else          _pre.Step(volts, _feat);
            // накопитель рекуррентный — одно нечисло испортило бы все
            // последующие кадры, поэтому санируем здесь
            for (int k = 0; k < NCh; k++)
            {
                float f = _feat[k];
                if (!IsFinite(f) || f < 0f) f = 0f;
                if (f > 4f) f = 4f;
                _acc[k] += f;
            }
            _accN++;

            if (++_sub < Decim) return _out;   // между вызовами сети держим прошлое
            _sub = 0;

            var x = new DenseTensor<float>(new[] { 1, NCh });
            float inv = 1f / _accN;
            for (int k = 0; k < NCh; k++) { x[0, k] = _acc[k] * inv; _acc[k] = 0f; }
            _accN = 0;

            var h = new DenseTensor<float>(new[] { 1, NState });
            for (int k = 0; k < NState; k++) h[0, k] = _state[k];

            using (var res = _sess.Run(new List<NamedOnnxValue> {
                       NamedOnnxValue.CreateFromTensor(_inX, x),
                       NamedOnnxValue.CreateFromTensor(_inH, h) }))
            {
                var map = res.ToDictionary(v => v.Name, v => v.AsTensor<float>());
                float y = map[_outY][0, 0];
                var hn = map[_outH];

                // Скрытое состояние заводится само в себя: одно нечисло в нём
                // отравляет выход навсегда. Проверяем ДО копирования обратно.
                // Обычный ограничитель тут не спасает — все сравнения с NaN
                // ложны, и «y > 1 ? 1 : y» пропускает NaN насквозь.
                bool bad = !IsFinite(y);
                for (int k = 0; k < NState && !bad; k++)
                    if (!IsFinite(hn[0, k])) bad = true;

                if (bad)
                {
                    Array.Clear(_state, 0, _state.Length);   // контекст наберётся заново
                    NanEvents++;
                }
                else
                {
                    for (int k = 0; k < NState; k++) _state[k] = hn[0, k];
                    _out = y < 0f ? 0f : (y > 1f ? 1f : y);
                }
            }
            return _out;
        }

        /// <summary>Сколько раз скрытое состояние пришлось обнулить из-за
        /// нечисла. В норме 0.</summary>
        public int NanEvents { get; private set; }

        // Проверка по битам, а не float.IsFinite: не зависит от режима
        // вычислений и одинакова с C-версией (nirs_nn_runtime.c).
        static bool IsFinite(float x)
        {
            uint u = (uint)BitConverter.SingleToInt32Bits(x);
            return (u & 0x7F800000u) != 0x7F800000u;
        }

        public void Dispose() => _sess?.Dispose();
    }

    // ------------------------------------------------------------------
    //  Обёртка под интерфейс из ExampleDynamic — чтобы работал
    //  ExampleDynamic.CompareWithModel(csv, model).
    // ------------------------------------------------------------------
    public sealed class OnnxModelAdapter : ExampleDynamic.INirsModel, IDisposable
    {
        readonly NirsOnnxModel _m;
        public OnnxModelAdapter(string path, float fs = 1000f) { _m = new NirsOnnxModel(path, fs); }
        public float Predict(float[] volts) => _m.Predict(volts);
        public void  Reset() => _m.Reset();
        public void  Dispose() => _m.Dispose();
    }
}

// ============================================================================
//  ПРИМЕР ИСПОЛЬЗОВАНИЯ
// ============================================================================
#if NIRS_ONNX_DEMO
static class OnnxDemo
{
    // 1. Простейший прогон файла через сеть
    public static void RunFile(string onnxPath, string csvPath)
    {
        using (var model = new Nirs.NirsOnnxModel(onnxPath))
        {
            foreach (var v in ReadFrames(csvPath))
            {
                float y = model.Predict(v);
                // y — уровень сокращения 0..1, model.OutVolts(3.3f) — в вольтах
                Console.WriteLine($"{y:F3}  {model.OutVolts(3.3f):F3} В");
            }
        }
    }

    // 2. Сеть и алгоритм рядом: считаем оба, пишем в файл, печатаем расхождение
    public static void CompareWithAlgorithm(string onnxPath, string csvPath)
    {
        var core = new Nirs.NirsContraction(new Nirs.NirsConfig { Fs = 1000f });
        using (var model = new Nirs.NirsOnnxModel(onnxPath))
        using (var w = new StreamWriter("compare_algo_vs_nn.csv"))
        {
            w.WriteLine("t_s;algo;nn;diff");
            double sum = 0, max = 0; int n = 0, i = 0, warm = 1500;
            foreach (var v in ReadFrames(csvPath))
            {
                float a = core.Update(v);
                float b = model.Predict(v);
                if (i >= warm)
                {
                    double d = Math.Abs(a - b);
                    sum += d; if (d > max) max = d; n++;
                }
                w.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0:F3};{1:F6};{2:F6};{3:F6}", i / 1000.0, a, b, b - a));
                i++;
            }
            Console.WriteLine($"сеть против алгоритма: MAE {sum / n:F4}, max|Δ| {max:F4}");
        }
    }

    // 3. Живой поток: то же самое, но кадры приходят с прибора
    public static void LiveStream(string onnxPath, Func<float[]> readFrame)
    {
        using (var model = new Nirs.NirsOnnxModel(onnxPath))
        {
            long n = 0;
            while (true)
            {
                float[] v = readFrame();           // блокирующее чтение кадра
                if (v == null) break;
                float y = model.Predict(v);
                if (++n % 100 == 0)                // печать 10 раз в секунду
                    Console.WriteLine($"{n / 1000.0,7:F2} с  {y,6:F3}  " +
                                      $"{model.OutVolts(3.3f),6:F3} В");
            }
        }
    }

    static IEnumerable<float[]> ReadFrames(string path)
    {
        using (var sr = new StreamReader(path))
        {
            sr.ReadLine();                          // заголовок
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                var p = line.Split(';');
                var v = new float[8];
                for (int k = 0; k < 8; k++)
                {
                    v[k] = float.NaN;
                    if (k >= p.Length) continue;
                    string t = p[k].Replace(',', '.').Trim();
                    if (t.Length == 0) continue;
                    float f;
                    if (float.TryParse(t, NumberStyles.Float,
                                       CultureInfo.InvariantCulture, out f)) v[k] = f;
                }
                yield return v;
            }
        }
    }
}
#endif
