// ============================================================================
//  NirsValidator.cs — проверка работы модели на ПК (сторона ПК).
//
//  Прогоняет записи через сеть и через детерминированный алгоритм, считает
//  метрики и печатает отчёт. Нужен, чтобы решить, годится ли обученная
//  модель для заливки в прошивку.
//
//  Метрики считаются ПО СОКРАЩЕНИЯМ, а не по выборкам. При 1 кГц соседние
//  выборки почти одинаковы, поэтому средняя ошибка по выборкам мало что
//  говорит: она смещена в сторону покоя, который занимает половину записи
//  и предсказывается тривиально.
// ============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NIRS_Demonstrator
{
    public sealed class ValidationResult
    {
        public string File;
        public int    Samples;
        public double Seconds;

        public double Mae;            // по всем выборкам после прогрева
        public double MaeRest;        // отдельно в покое
        public double MaeActive;      // отдельно на сокращении
        public double R;              // корреляция
        public int    ContractionsRef, ContractionsNn, Missed, False_;
        public double AmplitudeR;     // пропорциональность по амплитудам плато
        public double LatencyMedianMs;
        public double RestLevel, PlateauLevel;

        public override string ToString()
        {
            var s = new StringBuilder();
            s.AppendLine($"{Path.GetFileName(File)}  ({Seconds:F1} с)");
            s.AppendLine($"  MAE  всего {Mae:F4}   покой {MaeRest:F4}   сокращение {MaeActive:F4}");
            s.AppendLine($"  корреляция {R:F4}   пропорциональность амплитуд {AmplitudeR:F3}");
            s.AppendLine($"  сокращений: эталон {ContractionsRef}, сеть {ContractionsNn} " +
                         $"(пропущено {Missed}, лишних {False_})");
            s.AppendLine($"  задержка {LatencyMedianMs:+0;-0} мс   " +
                         $"уровень покоя {RestLevel:F3}, плато {PlateauLevel:F3}");
            return s.ToString();
        }
    }

    public static class NirsValidator
    {
        /// <summary>Прогон одной записи. reference — эталонный сигнал 0..1
        /// (цель из файла); predict — функция модели.</summary>
        public static ValidationResult Validate(
            string path, float[][] frames, float[] reference,
            Func<float[], float> predict, float fs = 1000f, float warmupS = 3f)
        {
            int n = Math.Min(frames.Length, reference.Length);
            var pred = new float[n];
            for (int i = 0; i < n; i++) pred[i] = predict(frames[i]);

            int w = (int)(warmupS * fs);
            var res = new ValidationResult {
                File = path, Samples = n, Seconds = n / fs
            };

            // --- ошибки по зонам ---
            double se = 0, sr = 0, sa = 0; int nr = 0, na = 0;
            for (int i = w; i < n; i++)
            {
                double d = Math.Abs(pred[i] - reference[i]);
                se += d;
                if (reference[i] < 0.15) { sr += d; nr++; }
                else if (reference[i] > 0.5) { sa += d; na++; }
            }
            int cnt = Math.Max(n - w, 1);
            res.Mae = se / cnt;
            res.MaeRest = nr > 0 ? sr / nr : 0;
            res.MaeActive = na > 0 ? sa / na : 0;
            res.R = Corr(pred, reference, w, n);
            res.RestLevel = Median(pred, reference, w, n, r => r < 0.15);
            res.PlateauLevel = Median(pred, reference, w, n, r => r > 0.5);

            // --- сегменты сокращений по эталону ---
            var seg = Segments(reference, w, n, 0.45f, 0.25f, (int)(0.25f * fs));
            var segP = Segments(pred, w, n, 0.45f, 0.25f, (int)(0.25f * fs));
            res.ContractionsRef = seg.Count;
            res.ContractionsNn = segP.Count;

            var lat = new List<double>();
            var ampR = new List<double>();
            var ampP = new List<double>();
            int matched = 0;
            foreach (var sg in seg)
            {
                // сопоставление по началу, окно +-300 мс
                bool found = false; int best = 0;
                foreach (var p in segP)
                    if (Math.Abs(p.A - sg.A) < 0.3 * fs) { found = true; best = p.A; break; }
                if (found)
                {
                    matched++;
                    lat.Add((best - sg.A) * 1000.0 / fs);
                }
                int p0 = sg.A + (int)(0.35 * (sg.B - sg.A));
                ampR.Add(MedianRange(reference, p0, sg.B) - Baseline(reference, sg.A, fs));
                ampP.Add(MedianRange(pred, p0, sg.B));
            }
            res.Missed = seg.Count - matched;
            res.False_ = Math.Max(0, segP.Count - matched);
            res.LatencyMedianMs = lat.Count > 0 ? Median(lat) : 0;
            res.AmplitudeR = ampR.Count > 2 ? Corr(ampP.ToArray(), ampR.ToArray()) : double.NaN;
            return res;
        }

        /// <summary>Свод по нескольким записям + вердикт.</summary>
        public static string Summary(IList<ValidationResult> rs,
                                     double maeLimit = 0.10,
                                     double ampLimit = 0.80)
        {
            var s = new StringBuilder();
            foreach (var r in rs) s.Append(r.ToString());

            double mae = rs.Average(r => r.Mae);
            double amp = rs.Where(r => !double.IsNaN(r.AmplitudeR))
                           .Select(r => r.AmplitudeR).DefaultIfEmpty(double.NaN).Average();
            int missed = rs.Sum(r => r.Missed), fals = rs.Sum(r => r.False_);
            int total  = rs.Sum(r => r.ContractionsRef);

            s.AppendLine("--------------------------------------------------");
            s.AppendLine($"ИТОГО по {rs.Count} записям:");
            s.AppendLine($"  средняя MAE            {mae:F4}   (порог {maeLimit:F2})");
            s.AppendLine($"  пропорциональность     {amp:F3}   (порог {ampLimit:F2})");
            s.AppendLine($"  пропущено сокращений   {missed} из {total}");
            s.AppendLine($"  ложных срабатываний    {fals}");
            s.AppendLine($"  разброс MAE по записям {rs.Min(r => r.Mae):F3}..{rs.Max(r => r.Mae):F3}");

            bool ok = mae <= maeLimit && (double.IsNaN(amp) || amp >= ampLimit)
                      && missed <= total * 0.05 && fals <= total * 0.05;
            s.AppendLine(ok ? "  ВЕРДИКТ: годится для заливки"
                            : "  ВЕРДИКТ: не годится — смотрите, какой критерий не выполнен");
            if (rs.Max(r => r.Mae) > 2 * mae)
                s.AppendLine("  ЗАМЕЧАНИЕ: одна из записей заметно хуже остальных. " +
                             "Скорее всего в обучении не было похожих условий.");
            return s.ToString();
        }

        // ------------------------------------------------------------------
        /// <summary>Отрезок сокращения. Обычная структура вместо кортежа:
        /// так собирается и старыми компиляторами (Unity, .NET Framework).</summary>
        struct Seg { public int A, B; public Seg(int a, int b) { A = a; B = b; } }

        static List<Seg> Segments(float[] y, int a, int b, float hi, float lo, int minLen)
        {
            var r = new List<Seg>();
            int st = 0, s0 = 0;
            for (int i = a; i < b; i++)
            {
                if (st == 0 && y[i] > hi) { st = 1; s0 = i; }
                else if (st == 1 && y[i] < lo)
                {
                    st = 0;
                    if (i - s0 >= minLen) r.Add(new Seg(s0, i));
                }
            }
            return r;
        }

        static double Baseline(float[] y, int s0, float fs)
        {
            int a = Math.Max(0, s0 - (int)(0.5 * fs)), b = Math.Max(a + 1, s0 - (int)(0.1 * fs));
            return MedianRange(y, a, b);
        }

        static double MedianRange(float[] y, int a, int b)
        {
            if (b <= a) return 0;
            var v = new float[b - a];
            Array.Copy(y, a, v, 0, b - a);
            Array.Sort(v);
            return v[v.Length / 2];
        }

        static double Median(List<double> v)
        {
            var s = v.OrderBy(x => x).ToArray();
            return s.Length == 0 ? 0 : s[s.Length / 2];
        }

        static double Median(float[] pred, float[] refr, int a, int b, Func<float, bool> sel)
        {
            var v = new List<float>();
            for (int i = a; i < b; i++) if (sel(refr[i])) v.Add(pred[i]);
            v.Sort();
            return v.Count == 0 ? 0 : v[v.Count / 2];
        }

        static double Corr(float[] x, float[] y, int a, int b)
        {
            double mx = 0, my = 0; int n = b - a;
            for (int i = a; i < b; i++) { mx += x[i]; my += y[i]; }
            mx /= n; my /= n;
            double sxy = 0, sxx = 0, syy = 0;
            for (int i = a; i < b; i++)
            {
                double dx = x[i] - mx, dy = y[i] - my;
                sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
            }
            return (sxx > 0 && syy > 0) ? sxy / Math.Sqrt(sxx * syy) : double.NaN;
        }

        static double Corr(double[] x, double[] y)
        {
            int n = Math.Min(x.Length, y.Length);
            double mx = x.Take(n).Average(), my = y.Take(n).Average();
            double sxy = 0, sxx = 0, syy = 0;
            for (int i = 0; i < n; i++)
            {
                double dx = x[i] - mx, dy = y[i] - my;
                sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
            }
            return (sxx > 0 && syy > 0) ? sxy / Math.Sqrt(sxx * syy) : double.NaN;
        }

        /// <summary>Чтение записи: 8 колонок вольт + 9-я цель.
        /// Понимает и заголовок, и его отсутствие, и хвостовой ';'.</summary>
        public static void ReadRecord(string path, out float[][] frames, out float[] target,
                                      float targetVmax)
        {
            var F = new List<float[]>();
            var T = new List<float>();
            using (var sr = new StreamReader(path))
            {
                string line = sr.ReadLine();
                if (line != null && !LooksNumeric(line)) line = sr.ReadLine();  // заголовок
                for (; line != null; line = sr.ReadLine())
                {
                    if (line.Length == 0) continue;
                    var p = line.Split(';');
                    var v = new float[8];
                    for (int k = 0; k < 8; k++) v[k] = k < p.Length ? Parse(p[k]) : float.NaN;
                    F.Add(v);
                    T.Add(p.Length > 8 ? Parse(p[8]) / targetVmax : 0f);
                }
            }
            frames = F.ToArray();
            target = T.Select(t => float.IsNaN(t) ? 0f : Math.Min(1f, Math.Max(0f, t))).ToArray();
        }

        static bool LooksNumeric(string line)
        {
            float f;
            return float.TryParse(line.Split(';')[0].Replace(',', '.').Trim(),
                                  NumberStyles.Float, CultureInfo.InvariantCulture, out f);
        }

        static float Parse(string t)
        {
            t = t.Replace(',', '.').Trim();
            float f;
            return t.Length > 0 && float.TryParse(t, NumberStyles.Float,
                                                  CultureInfo.InvariantCulture, out f)
                   ? f : float.NaN;
        }
    }
}

// ============================================================================
//  ПРИМЕР
// ============================================================================
#if NIRS_VALIDATOR_DEMO
static class ValidatorDemo
{
    public static void Run(string onnxPath, string[] records, float targetVmax)
    {
        var results = new System.Collections.Generic.List<Nirs.ValidationResult>();
        foreach (var rec in records)
        {
            float[][] frames; float[] target;
            Nirs.NirsValidator.ReadRecord(rec, out frames, out target, targetVmax);

            using (var model = new Nirs.NirsOnnxModel(onnxPath))
            {
                model.Reset();
                results.Add(Nirs.NirsValidator.Validate(
                    rec, frames, target, v => model.Predict(v)));
            }
        }
        Console.WriteLine(Nirs.NirsValidator.Summary(results));
    }

    // то же для детерминированного алгоритма — базовая линия для сравнения
    public static void RunAlgorithm(string[] records, float targetVmax)
    {
        var results = new System.Collections.Generic.List<Nirs.ValidationResult>();
        foreach (var rec in records)
        {
            float[][] frames; float[] target;
            Nirs.NirsValidator.ReadRecord(rec, out frames, out target, targetVmax);
            var core = new Nirs.NirsContraction(new Nirs.NirsConfig { Fs = 1000f });
            results.Add(Nirs.NirsValidator.Validate(rec, frames, target, v => core.Update(v)));
        }
        Console.WriteLine(Nirs.NirsValidator.Summary(results));
    }
}
#endif
