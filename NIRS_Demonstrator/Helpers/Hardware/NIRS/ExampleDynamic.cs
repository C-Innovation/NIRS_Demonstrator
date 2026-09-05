// ============================================================================
//  ExampleDynamic.cs — примеры вызова ядра в динамике на C#.
//
//  Пять сценариев:
//    1. Минимальный цикл.
//    2. Поток из последовательного порта (реальный прибор).
//    3. Прогон файла с воспроизведением темпа реального времени.
//    4. Онлайн-инференс нейросети рядом с алгоритмом (сверка выходов).
//    5. Калибровка и сохранение шкалы.
// ============================================================================
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
#if NIRS_SERIAL
using System.IO.Ports;   // .NET: dotnet add package System.IO.Ports
#endif
using NIRS_Demonstrator;

public static class ExampleDynamic
{
    // ------------------------------------------------------------------
    //  1. МИНИМАЛЬНЫЙ ЦИКЛ
    // ------------------------------------------------------------------
    public static void Minimal()
    {
        var cfg = new NirsConfig { Fs = 1000f, OutVmax = 3.30f };
        var core = new NirsContraction(cfg);

        var v = new float[NirsConfig.MaxCh];
        for (int i = 0; i < 10000; i++)
        {
            GetPhotodiodeVolts(v);              // заполнить v[0..7], вольты

            float level = core.Update(v);       // 0..1
            float volts = core.OutVolts;        // 0..OutVmax

            if ((core.Flags & NirsFlags.Ready) != 0)
                Console.WriteLine($"{level:F3}  {volts:F3} В");
        }
    }

    // ------------------------------------------------------------------
    //  2. ПОТОК ИЗ ПОСЛЕДОВАТЕЛЬНОГО ПОРТА
    //
    //  Кадр: 8 значений uint16 (коды АЦП) + 2 байта синхромаркера.
    //  Ядро вызывается ровно один раз на кадр.
    //
    //  Собирается с -define:NIRS_SERIAL; на .NET нужен пакет
    //  System.IO.Ports (dotnet add package System.IO.Ports).
    // ------------------------------------------------------------------
#if NIRS_SERIAL
    public static void FromSerialPort(string portName, int baud = 921600)
    {
        const int AdcBits = 12;
        const float AdcVref = 3.30f, AfeDiv = 2.0f;
        float k = AdcVref * AfeDiv / ((1 << AdcBits) - 1);

        var core = new NirsContraction(new NirsConfig { Fs = 1000f });
        var v = new float[NirsConfig.MaxCh];
        var frame = new byte[16];

        using (var port = new SerialPort(portName, baud))
        {
            port.Open();
            port.ReadTimeout = 2000;

            long n = 0;
            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (!ReadExactly(port, frame, frame.Length)) continue;

                for (int i = 0; i < NirsConfig.MaxCh; i++)
                {
                    ushort code = (ushort)(frame[2 * i] | (frame[2 * i + 1] << 8));
                    v[i] = code * k;
                }

                float o = core.Update(v);
                n++;

                // печать 20 раз в секунду, расчёт — все 1000
                if (n % 50 == 0)
                {
                    string st = (core.Flags & NirsFlags.Ready) == 0 ? "прогрев"
                              : (core.Flags & NirsFlags.NoChan) != 0 ? "НЕТ СИГНАЛА"
                              : (core.Flags & NirsFlags.Active) != 0 ? "сокращение"
                              : "покой";
                    Console.WriteLine($"{sw.Elapsed.TotalSeconds,7:F1} с  " +
                                      $"{o,6:F3}  {core.OutVolts,6:F3} В  {st}");
                }
            }
        }
    }

    static bool ReadExactly(SerialPort p, byte[] buf, int len)
    {
        int got = 0;
        try
        {
            while (got < len) got += p.Read(buf, got, len - got);
            return true;
        }
        catch (TimeoutException) { return false; }
    }
#endif  // NIRS_SERIAL

    // ------------------------------------------------------------------
    //  3. ПРОГОН ФАЙЛА В ТЕМПЕ РЕАЛЬНОГО ВРЕМЕНИ
    //
    //  Полезно, чтобы посмотреть поведение алгоритма «как вживую»,
    //  не подключая прибор.
    // ------------------------------------------------------------------
    public static void ReplayFile(string csvPath, bool realtime = true)
    {
        string[] hdr;
        var data = LoadCsv(csvPath, out hdr);
        var core = new NirsContraction(new NirsConfig { Fs = 1000f });

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < data.Length; i++)
        {
            float o = core.Update(data[i]);

            if (realtime)
            {
                // выдерживаем 1 кГц; Thread.Sleep слишком грубый, поэтому
                // ждём по счётчику и уступаем квант при большом отставании
                double due = i / 1000.0;
                while (sw.Elapsed.TotalSeconds < due)
                {
                    double lag = due - sw.Elapsed.TotalSeconds;
                    if (lag > 0.002) Thread.Sleep(1); else Thread.SpinWait(50);
                }
            }
            if (i % 100 == 0) OnNewValue(i / 1000.0, o, core.Flags);
        }
    }

    static void OnNewValue(double t, float level, NirsFlags fl)
    {
        // сюда подставьте обновление графика / отправку в UI
        Console.WriteLine($"{t,7:F2} с  {level,6:F3}  {fl}");
    }

    // ------------------------------------------------------------------
    //  4. НЕЙРОСЕТЬ РЯДОМ С АЛГОРИТМОМ
    //
    //  Считаем оба выхода на одном потоке и сравниваем. Так удобно
    //  проверять обученную модель до заливки в микроконтроллер.
    //  (INirsModel — интерфейс к ONNX Runtime или TFLite, см. §7 README.)
    // ------------------------------------------------------------------
    public interface INirsModel
    {
        /// <summary>8 напряжений -> предсказание 0..1. Модель хранит своё
        /// состояние внутри (рекуррентная), поэтому вызывать строго по порядку.</summary>
        float Predict(float[] volts);
        void Reset();
    }

    public static void CompareWithModel(string csvPath, INirsModel model)
    {
        string[] hdr;
        var data = LoadCsv(csvPath, out hdr);
        var core = new NirsContraction(new NirsConfig { Fs = 1000f });
        model.Reset();

        double sumAbs = 0, maxAbs = 0;
        int n = 0, warm = 1500;
        using (var w = new StreamWriter("compare_algo_vs_nn.csv"))
        {
            w.WriteLine("t_s;algo;nn;diff");
            for (int i = 0; i < data.Length; i++)
            {
                float a = core.Update(data[i]);
                float b = model.Predict(data[i]);
                if (i >= warm)
                {
                    double d = Math.Abs(a - b);
                    sumAbs += d; if (d > maxAbs) maxAbs = d; n++;
                }
                w.WriteLine("{0};{1};{2};{3}",
                    (i / 1000.0).ToString("F3", CultureInfo.InvariantCulture),
                    a.ToString("F6", CultureInfo.InvariantCulture),
                    b.ToString("F6", CultureInfo.InvariantCulture),
                    (b - a).ToString("F6", CultureInfo.InvariantCulture));
            }
        }
        Console.WriteLine($"сеть против алгоритма: MAE = {sumAbs / n:F4}, max|Δ| = {maxAbs:F4}");
    }

    // ------------------------------------------------------------------
    //  5. КАЛИБРОВКА
    // ------------------------------------------------------------------
    public static void CalibrationFlow(NirsContraction core)
    {
        Console.WriteLine("Сожмите мышцу максимально и удерживайте 2 секунды...");
        Thread.Sleep(2000);
        core.CalibrateMvc();                       // фиксируем текущий уровень как 100 %
        File.WriteAllText("mvc.txt", core.Mvc.ToString("R", CultureInfo.InvariantCulture));
        Console.WriteLine($"калибровка сохранена: MVC = {core.Mvc:F4} OD");
    }

    public static void RestoreCalibration(NirsContraction core)
    {
        if (!File.Exists("mvc.txt")) return;
        float mvc;
        if (float.TryParse(File.ReadAllText("mvc.txt"), NumberStyles.Float,
                           CultureInfo.InvariantCulture, out mvc) && mvc > 0 && mvc < 10)
            core.SetMvc(mvc);
    }

    // ------------------------------------------------------------------
    static void GetPhotodiodeVolts(float[] v) { /* ваш источник данных */ }

    static float[][] LoadCsv(string path, out string[] header)
    {
        var rows = new System.Collections.Generic.List<float[]>();
        using (var sr = new StreamReader(path))
        {
            header = sr.ReadLine().Split(';');
            int nch = Math.Min(header.Length, NirsConfig.MaxCh);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                var p = line.Split(';');
                if (p.Length < nch) continue;
                var v = new float[NirsConfig.MaxCh];
                for (int k = 0; k < NirsConfig.MaxCh; k++) v[k] = float.NaN;
                for (int k = 0; k < nch; k++)
                {
                    string t = p[k].Replace(',', '.').Trim();
                    if (t.Length == 0) continue;           // пустое поле = канал отключён
                    float f;
                    if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                        v[k] = f;
                }
                rows.Add(v);
            }
        }
        return rows.ToArray();
    }
}
