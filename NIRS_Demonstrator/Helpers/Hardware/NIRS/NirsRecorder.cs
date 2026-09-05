// ============================================================================
//  NirsRecorder.cs — сбор данных для датасета (сторона ПК).
//
//  Пишет запись в формате, который читают скрипты обучения, и кладёт рядом
//  json с метаданными. Без метаданных запись в датасет не попадёт: там
//  лежит subject (разбиение на обучение и контроль идёт ПО ЛЮДЯМ) и
//  target_vmax (полная шкала цели — она меняется между сборками прошивки,
//  и без неё одно и то же сокращение получит разные метки).
//
//  Три режима записи цели:
//    Algorithm — цель считает встроенный NirsContraction. Быстро начать,
//                но потолок качества задан самим алгоритмом.
//    External  — цель приходит извне (динамометр, ЭМГ). То, к чему надо
//                стремиться: сеть сможет выучить то, чего в алгоритме нет.
//    Protocol  — цель задаётся сценарием записи (метроном «сожми/отпусти»).
//                Годится для грубой разметки, когда датчика силы нет.
//
//  Формат файла — 9 колонок через ';', десятичная точка:
//      740_5mm;740_10mm;740_26mm;740_32mm;850_5mm;850_10mm;850_26mm;850_32mm;out_Y
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace NIRS_Demonstrator
{
    public enum TargetSource
    {
        Algorithm,   // цель = выход NirsContraction
        External,    // цель приходит вызывающим кодом (динамометр/ЭМГ)
        Protocol     // цель = сценарий записи
    }

    public sealed class RecorderConfig
    {
        public string Subject   = null;      // ОБЯЗАТЕЛЬНО
        public string Session   = null;      // по умолчанию — дата и время
        public string Muscle    = "";
        public string Placement = "1";       // номер установки датчика
        public string Notes     = "";
        public float  Fs        = 1000f;
        public float  TargetVmax = 3.30f;    // полная шкала цели, В
        public TargetSource Source = TargetSource.Algorithm;
        public string OutDir    = ".";
    }

    public sealed class NirsRecorder : IDisposable
    {
        readonly RecorderConfig _c;
        readonly NirsContraction _core;
        readonly StreamWriter _w;
        readonly string _csvPath, _jsonPath;
        readonly StringBuilder _sb = new StringBuilder(160);

        long _n;
        double _sumY;
        int _contractions;
        bool _prevActive;

        // Возврат в покой между сокращениями. Именно он, а не средний
        // уровень, показывает, успевают ли адаптивные фильтры найти базу:
        // при коротких паузах выход не доходит до нуля, соседние сокращения
        // сливаются, и запись становится непригодной для обучения.
        float _minBetween = 1f;
        readonly List<float> _minima = new List<float>();
        long _restSamples;

        public string CsvPath => _csvPath;
        public long Samples   => _n;
        public int  Contractions => _contractions;
        public double Seconds => _n / _c.Fs;

        public NirsRecorder(RecorderConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.Subject))
                throw new ArgumentException(
                    "не задан Subject. Разбиение на обучение и контроль идёт по " +
                    "людям; без этого поля запись бесполезна для датасета.");
            _c = cfg;
            if (string.IsNullOrEmpty(_c.Session))
                _c.Session = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            Directory.CreateDirectory(_c.OutDir);
            string bas = Path.Combine(_c.OutDir, $"{_c.Subject}_{_c.Session}");
            _csvPath = bas + ".csv";
            _jsonPath = bas + ".json";

            _core = new NirsContraction(new NirsConfig { Fs = _c.Fs, OutVmax = _c.TargetVmax });
            _w = new StreamWriter(_csvPath, false, new UTF8Encoding(false), 1 << 16);
            _w.WriteLine("740_5mm;740_10mm;740_26mm;740_32mm;" +
                         "850_5mm;850_10mm;850_26mm;850_32mm;out_Y");
        }

        /// <summary>Одна выборка. externalTarget нужен только в режиме
        /// External / Protocol и задаётся в тех же вольтах (0..TargetVmax).</summary>
        public void Write(float[] volts, float externalTarget = 0f)
        {
            float y;
            float algo = _core.Update(volts);   // считаем всегда: нужны флаг Active и контроль возврата в покой
            switch (_c.Source)
            {
                case TargetSource.Algorithm: y = _core.OutVolts; break;
                default:                     y = externalTarget; break;
            }

            _sb.Clear();
            for (int k = 0; k < 8; k++)
            {
                _sb.Append(volts[k].ToString("F4", CultureInfo.InvariantCulture));
                _sb.Append(';');
            }
            _sb.Append(y.ToString("F4", CultureInfo.InvariantCulture));
            _w.WriteLine(_sb.ToString());

            bool act = (_core.Flags & NirsFlags.Active) != 0;
            if (act && !_prevActive)
            {
                _contractions++;
                if (_contractions > 1) _minima.Add(_minBetween);
                _minBetween = 1f;
            }
            if (!act)
            {
                if (algo < _minBetween) _minBetween = algo;
                if (algo < 0.05f) _restSamples++;
            }
            _prevActive = act;
            _sumY += y;
            _n++;
        }

        /// <summary>Закрывает файл и пишет метаданные. Возвращает отчёт о
        /// пригодности записи.</summary>
        public string Finish()
        {
            _w.Flush(); _w.Dispose();

            var meta = new StringBuilder();
            meta.AppendLine("{");
            meta.AppendLine($"  \"subject\":     \"{Esc(_c.Subject)}\",");
            meta.AppendLine($"  \"session\":     \"{Esc(_c.Session)}\",");
            meta.AppendLine($"  \"fs\":          {_c.Fs.ToString("0.#", CultureInfo.InvariantCulture)},");
            meta.AppendLine($"  \"target_col\":  8,");
            meta.AppendLine($"  \"target_vmax\": {_c.TargetVmax.ToString("0.####", CultureInfo.InvariantCulture)},");
            meta.AppendLine($"  \"target_src\":  \"{_c.Source}\",");
            meta.AppendLine($"  \"muscle\":      \"{Esc(_c.Muscle)}\",");
            meta.AppendLine($"  \"placement\":   \"{Esc(_c.Placement)}\",");
            meta.AppendLine($"  \"duration_s\":  {Seconds.ToString("0.###", CultureInfo.InvariantCulture)},");
            meta.AppendLine($"  \"contractions\":{_contractions},");
            meta.AppendLine($"  \"notes\":       \"{Esc(_c.Notes)}\"");
            meta.Append("}");
            File.WriteAllText(_jsonPath, meta.ToString(), new UTF8Encoding(false));

            return Report();
        }

        /// <summary>Проверка записи по тем же критериям, что и в DATASET.md.
        /// Смотреть ДО того, как снимать следующего человека.</summary>
        public string Report()
        {
            var r = new StringBuilder();
            double duty = _n > 0 ? _sumY / _n / _c.TargetVmax : 0;
            double restFrac = _n > 0 ? (double)_restSamples / _n : 0;
            double medMin = 1.0;
            if (_minima.Count > 0)
            {
                var v = _minima.ToArray(); Array.Sort(v);
                medMin = v[v.Length / 2];
            }
            r.AppendLine($"запись {_csvPath}");
            r.AppendLine($"  длительность {Seconds:F1} с, сокращений {_contractions}");
            r.AppendLine($"  доля активности {100 * duty:F0} %, в покое {100 * restFrac:F0} % времени");
            r.AppendLine($"  выход между сокращениями опускается до {medMin:F2} (медиана)");

            if (Seconds < 60)
                r.AppendLine("  МАЛО: меньше минуты. Ориентир — 3-5 минут на установку.");
            if (_contractions < 20)
                r.AppendLine($"  МАЛО сокращений ({_contractions}). Нужно хотя бы 20, " +
                             "лучше 40, и разной силы.");
            // Главная проверка. Средний уровень тут обманчив: на реальной
            // записи с паузами 0.95 с он был всего 0.5, а выход при этом
            // ни разу не опустился ниже 0.19 — база не нашла покой, и
            // соседние сокращения слились в одно.
            if (medMin > 0.10)
                r.AppendLine($"  БРАК: между сокращениями выход не опускается ниже {medMin:F2}. " +
                             "Паузы слишком короткие, база не успевает найти покой, " +
                             "соседние сокращения сливаются. Делайте паузы 2-3 с и " +
                             "перепишите.");
            else if (medMin > 0.05)
                r.AppendLine($"  на грани: между сокращениями выход доходит только до {medMin:F2}. " +
                             "Паузы стоит удлинить до 2-3 с.");
            if (restFrac < 0.10)
                r.AppendLine($"  МАЛО ПОКОЯ: всего {100 * restFrac:F0} % времени в покое, " +
                             "нужно хотя бы 15 %.");
            if (duty > 0.7)
                r.AppendLine("  ПЛОТНО: доля активности выше 70 %.");
            if (duty < 0.1)
                r.AppendLine("  СЛИШКОМ РЕДКО: почти нет сокращений.");
            var f = _core.Flags;
            if ((f & NirsFlags.NoChan) != 0)
                r.AppendLine("  НЕТ СИГНАЛА: датчик отклеился или все каналы вне диапазона.");
            if ((f & NirsFlags.Sat) != 0)
                r.AppendLine("  насыщение: часть каналов у предела ОУ (это ожидаемо для " +
                             "5.5 и 10 мм до правки номиналов).");
            return r.ToString();
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void Dispose() { try { _w?.Dispose(); } catch { } }
    }
}

// ============================================================================
//  ПРИМЕР: сбор записи по сценарию с метрономом
// ============================================================================
#if NIRS_RECORDER_DEMO
static class RecorderDemo
{
    // Сценарий: 3 с покоя, затем чередование «держи 2 с / отпусти 2 с».
    // Каждый четвёртый цикл — вполсилы, чтобы в датасете были разные
    // амплитуды: без них проверить пропорциональность будет не на чем.
    public static void RecordProtocol(string subject, int cycles = 40)
    {
        var cfg = new Nirs.RecorderConfig {
            Subject = subject,
            Muscle  = "biceps",
            Placement = "1",
            Source  = Nirs.TargetSource.Algorithm,
            TargetVmax = 3.30f,
            OutDir  = "dataset"
        };

        using (var rec = new Nirs.NirsRecorder(cfg))
        {
            var v = new float[8];
            int phase = 0, held = 0;
            while (rec.Contractions < cycles && ReadFrame(v))
            {
                rec.Write(v);

                // подсказка испытуемому: печатаем команду при смене фазы
                if (++held >= 2000)                    // 2 с при 1 кГц
                {
                    held = 0; phase++;
                    bool squeeze = (phase % 2) == 1;
                    string force = (phase / 2) % 4 == 3 ? " ВПОЛСИЛЫ" : " СИЛЬНО";
                    Console.WriteLine(squeeze ? "СОЖМИ" + force : "отпусти");
                }
            }
            Console.WriteLine(rec.Finish());
        }
    }

    static bool ReadFrame(float[] v) => false;   // ваш источник данных
}
#endif
