// ============================================================================
//  NirsContraction.cs
//
//  Точный порт ядра nirs_contraction.c на C#. Порядок арифметических операций
//  сохранён 1:1, тип float — чтобы результат совпадал с прошивкой
//  микроконтроллера. Предназначен для проверки алгоритма на ПК и для
//  подготовки данных нейросети.
//
//  Расхождение с C-версией на приложенной записи: max|Δ| < 1e-6.
// ============================================================================
using System;

namespace NIRS_Demonstrator
{
    [Flags]
    public enum NirsFlags : ushort
    {
        None    = 0,
        Ready   = 0x0001,   // прогрев закончен
        Active  = 0x0002,   // идёт сокращение (дискретный детектор)
        NoChan  = 0x0004,   // нет пригодных каналов
        Sat     = 0x0008,   // канал в насыщении
        LowSig  = 0x0010,   // канал без модуляции
        Clipped = 0x0020,   // выход упёрся в 1.0
        Stable  = 0x0040,   // сошёлся и масштаб: уровню можно верить
        Tracking= 0x0080    // датчик на мышце, форма сигнала верна
    }

    /// <summary>Конфигурация. Все постоянные времени в секундах.</summary>
    public sealed class NirsConfig
    {
        public const int MaxCh = 8;

        public float Fs = 1000f;
        public int   Nch = 8;

        public float[] DistMm = { 5.5f, 10f, 26.5f, 32f, 5.5f, 10f, 26.5f, 32f };
        public float[] WlNm   = { 740f, 740f, 740f, 740f, 850f, 850f, 850f, 850f };

        public float VMin = 0.02f;
        public float VMax = 4.30f;      // OPA2380 @5В насыщается около 4.4 В
        // Гистерезис возврата канала в рабочий диапазон: выпавший канал снова
        // считается пригодным только при VMin+VHyst < V < VMax-VHyst.
        public float VHyst = 0.05f;

        public float TauBaseUp = 0.75f;
        public float TauBaseDn = 120f;
        public float FreezeK   = 4f;

        // Медленный спуск базы защищает её от «съедания» сокращением, но он же
        // мешает догонять дрейф уровня покоя. У канала обратной полярности это
        // критично: дрейф и сокращение двигают ослабление в одну сторону.
        // Различить их по самому каналу нельзя, зато можно по дальним: они
        // возвращаются в покой всегда. Пока их согласие ниже BaseRestThr, база
        // любого канала догоняет дрейф на быстрой постоянной в обе стороны.
        public float BaseRestThr = 0.15f;

        public float TauSpanUp = 2f;
        public float TauSpanDn = 60f;
        public float MinSpan   = 0.010f;

        public float TauNoise = 1f;
        public float SnrK     = 6f;
        public float WMin     = 0.05f;

        public float LeadTau   = 0.030f;
        public float TauLeadLp = 0.030f;

        public float TauAttack  = 0.010f;
        public float TauRelease = 0.060f;

        // MVC отражает УСТОЙЧИВЫЙ уровень сильного сокращения, а не выброс
        // на фронте (он в ~1.5 раза выше плато): детектор питается
        // дополнительно сглаженным сигналом.
        public float TauMvcLp = 0.15f;
        public float TauMvcUp = 0.30f;
        public float TauMvcDn = 300f;
        public float MvcMin   = 0.05f;
        // Плато отображается линейно в 0..SoftKnee, выбросы плавно
        // сжимаются в остаток шкалы. 1.0 -> жёсткое ограничение.
        public float SoftKnee = 0.85f;

        public float Deadzone = 0.030f;
        public float GateHi   = 0.30f;
        public float GateLo   = 0.15f;
        public float OutVmax  = 3.30f;
        public float WarmupS  = 1.5f;

        // --- признаки готовности: Tracking и Stable --------------------------
        // Сходятся ДВЕ РАЗНЫЕ вещи. Измерено на записях S01-S04 (ядро с
        // холодного старта против ядра, прогретого с начала записи):
        //   форма выхода (корреляция > 0.95)     5-19 с, в среднем 10 с
        //   уровень выхода (|dy| < 0.05)        23-61 с, в среднем 48 с
        // Форму задают база, полярность и размахи каналов, уровень — авто-
        // калибровка максимума (MVC). Поэтому флага два: Tracking (быстрый,
        // «датчик на мышце, сигнал живой») и Stable (медленный, «масштаб
        // устоялся»). Tracking считается по здоровью каналов, а не по
        // времени: выпад одного канала отнимает лишь его долю и флаг не
        // роняет, а снятие датчика роняет все каналы разом.
        public float TrackS        = 1.5f;    // с, непрерывной пригодности канала
        public float HealthHi      = 0.50f;   // порог подъёма Tracking
        public float HealthLo      = 0.35f;   // порог падения Tracking
        public float TauWRef       = 60.0f;   // с, спад опорной суммы весов
        public float MvcSettleS    = 10.0f;   // с, окно проверки масштаба
        public float MvcSettleThr  = 0.05f;   // допустимый дрейф масштаба за окно

        // --- автоматическая полярность каналов -------------------------------
        // На малых разносах (5.5 мм) при сокращении света становится МЕНЬШЕ, а
        // на больших (26.5 и 32 мм) — больше. Ядро меряет активность как
        // «насколько ослабление ниже покоя», поэтому канал обратного знака без
        // поправки даёт инвертированную активность: максимум в покое и ноль под
        // нагрузкой. Один такой канал поднимает пол выхода, два — рушат выход.
        // Полярность дальних каналов принимается опорной (+1); знак ближних
        // оценивается по корреляции с дальними и при необходимости меняется.
        public bool  AutoPolarity = true;
        public float TauPol       = 5.0f;    // с, постоянная оценки корреляции
        public float PolThr       = 0.35f;   // порог смены знака по корреляции
        public float PolHoldS     = 10f;     // с, пауза между сменами знака

        public NirsConfig Clone()
        {
            var c = (NirsConfig)MemberwiseClone();
            c.DistMm = (float[])DistMm.Clone();
            c.WlNm   = (float[])WlNm.Clone();
            return c;
        }
    }

    public sealed class NirsContraction
    {
        public const int MaxCh = NirsConfig.MaxCh;

        readonly NirsConfig _c;
        readonly int _nch;

        // коэффициенты фильтров
        readonly float _aBaseUp, _aBaseDn, _aSpanUp, _aSpanDn, _aNoise;
        readonly float _aAtt, _aRel, _aMvcLp, _aMvcUp, _aMvcDn, _aLead, _leadGain;
        readonly uint  _warmupN;
        readonly bool[] _isFar = new bool[MaxCh];
        readonly bool[] _is850 = new bool[MaxCh];

        // состояние
        readonly float[] _base  = new float[MaxCh];
        readonly float[] _span  = new float[MaxCh];
        readonly float[] _noise = new float[MaxCh];
        readonly float[] _prev  = new float[MaxCh];
        readonly float[] _od    = new float[MaxCh];
        readonly float[] _w     = new float[MaxCh];
        // 0 — непригоден, 1 — ждёт покоя после возврата, 2 — работает нормально
        readonly byte[]  _chOk  = new byte[MaxCh];
        // 1 — канал переучивает базу и ещё не вернулся в слияние
        readonly byte[]  _rearm   = new byte[MaxCh];
        readonly byte[]  _chReady = new byte[MaxCh];
        readonly uint[]  _chRun   = new uint[MaxCh];
        readonly float[] _pol     = new float[MaxCh];
        readonly float[] _polCov  = new float[MaxCh];
        readonly float[] _polMdn  = new float[MaxCh];
        readonly float[] _polVdn  = new float[MaxCh];
        readonly uint[]  _polLock = new uint[MaxCh];
        readonly float[] _dn      = new float[MaxCh];
        float _polMref, _polVref, _aPol, _farAct, _farG;
        uint  _polHoldN;

        uint  _n, _trackN, _mvcSettleN, _mvcMarkN;
        float _aWRef, _wRef, _health, _mvcMark, _stableProgress;
        bool  _tracking, _mvcSettled;
        float _uLp, _level, _mvc, _mvcLp, _spatial, _spectral, _uOd, _wsum;
        float _out01, _outV;
        NirsFlags _flags;

        // ---- публичные результаты последней выборки ----
        public float     Out01    => _out01;                 // 0..1
        public float     OutVolts => _outV;                  // 0..OutVmax
        public ushort    Dac12    => (ushort)Math.Min(4095f, Math.Max(0f, _out01 * 4095f + 0.5f));
        public float     UOd      => _uOd;                   // слитый сигнал, единицы OD
        public float     Level    => _level;                 // сглаженный уровень, OD
        public float     Mvc      => _mvc;
        public float     Spatial  => _spatial;               // дальние минус ближние
        public float     Spectral => _spectral;              // 850 минус 740
        public float     WSum     => _wsum;
        public NirsFlags Flags    => _flags;

        /// <summary>
        /// БЫСТРЫЙ признак: датчик на мышце, каналы набрали базу и знак, выход
        /// правильно повторяет мышцу. Поднимается за единицы секунд, падает
        /// в ту же выборку, когда датчик сняли. Именно его брать для индикации
        /// и для гейта выхода.
        /// </summary>
        public bool  Tracking       => (_flags & NirsFlags.Tracking) != 0;

        /// <summary>
        /// МЕДЛЕННЫЙ признак: вдобавок к Tracking устоялся масштаб
        /// (автокалибровка максимума). Нужен там, где важна абсолютная
        /// величина: разметка датасета, пороги в долях от максимума.
        /// </summary>
        public bool  Stable         => (_flags & NirsFlags.Stable) != 0;

        /// <summary>Здоровье установки, 0..1: доля веса работающих каналов
        /// относительно того, сколько их тут обычно работает. 1.0 — все на
        /// месте, ~0.8 — выпал один из пяти, 0 — датчик снят.</summary>
        public float Health         => _health;

        /// <summary>Приблизительный ход адаптации, 0..1 — для полоски прогресса.</summary>
        public float StableProgress => _stableProgress;
        public float[]   Od       => _od;
        public float[]   W        => _w;
        public float[]   Span     => _span;
        public float[]   Pol      => _pol;      // +1 или -1 по каналам

        /// <summary>
        /// Признаки для нейросети: нормированная активность каждого канала,
        /// od/span, с ограничением сверху на 4. Это то же самое, что раньше
        /// считал отдельный предфильтр (NirsPrefilter), но здесь уже учтены
        /// полярность канала и слежение базы за дрейфом, которых в предфильтре
        /// не было. На записях 4 сентября замена дала MAE 0.109 против 0.136
        /// и наклон регрессии амплитуд 0.32 против 0.19.
        /// Вызывать после Update() того же шага.
        /// </summary>
        /// <summary>
        /// Использовать тот же быстрый логарифм, что и прошивка, собранная с
        /// -DNIRS_FAST_LOG. Нужно, когда признаки для обучения готовятся на ПК,
        /// а работать модель будет на МК: обычный Math.Log считает в double и
        /// даёт чуть другой результат, а решения внутри ядра дискретные (смена
        /// полярности, переарм канала), поэтому «чуть другой» со временем
        /// расходится в заметный. С этим флагом C и C# совпадают побитово.
        /// </summary>
        public static bool UseFastLog = false;

        static float FastLog(float x)
        {
            // Копия nirs_logf() из nirs_contraction.c при NIRS_FAST_LOG.
            int bits = BitConverter.ToInt32(BitConverter.GetBytes(x), 0);
            int e = ((bits >> 23) & 0xFF) - 127;
            int mb = (bits & 0x007FFFFF) | 0x3F800000;
            float m = BitConverter.ToSingle(BitConverter.GetBytes(mb), 0);
            if (m > 1.41421356f) { m *= 0.5f; e += 1; }
            float t = (m - 1f) / (m + 1f);
            float t2 = t * t;
            return 2f * t * (1f + t2 * (0.33333333f + t2 * (0.2f
                        + t2 * (0.14285714f + t2 * 0.11111111f))))
                   + (float)e * 0.69314718f;
        }

        static float Log(float x) { return UseFastLog ? FastLog(x) : (float)Math.Log(x); }

        public void NnFeatures(float[] outv)
        {
            for (int k = 0; k < _nch; k++)
            {
                float sp = _span[k] < _c.MinSpan ? _c.MinSpan : _span[k];
                float v = _w[k] > 0f ? _od[k] / sp : 0f;
                outv[k] = v > 4f ? 4f : v;
            }
        }
        public float[]   Noise    => _noise;
        public NirsConfig Config  => _c;

        public NirsContraction(NirsConfig cfg = null)
        {
            _c   = (cfg ?? new NirsConfig()).Clone();
            float fs = _c.Fs > 0f ? _c.Fs : 1000f;
            _nch = Math.Min(Math.Max(_c.Nch, 1), MaxCh);

            _aBaseUp = Coef(fs, _c.TauBaseUp);
            _aBaseDn = Coef(fs, _c.TauBaseDn);
            _aSpanUp = Coef(fs, _c.TauSpanUp);
            _aSpanDn = Coef(fs, _c.TauSpanDn);
            _aNoise  = Coef(fs, _c.TauNoise);
            _aAtt    = Coef(fs, _c.TauAttack);
            _aRel    = Coef(fs, _c.TauRelease);
            _aMvcLp  = Coef(fs, _c.TauMvcLp);
            _aMvcUp  = Coef(fs, _c.TauMvcUp);
            _aMvcDn  = Coef(fs, _c.TauMvcDn);
            _aLead   = Coef(fs, _c.TauLeadLp);
            _leadGain = _c.TauLeadLp > 0f ? _c.LeadTau / _c.TauLeadLp : 0f;
            _warmupN     = (uint)(_c.WarmupS * fs);
            _trackN      = (uint)(_c.TrackS * fs);
            _mvcSettleN  = (uint)(_c.MvcSettleS * fs);
            _aWRef       = Coef(fs, _c.TauWRef);
            _aPol     = Coef(fs, _c.TauPol);
            _polHoldN = (uint)(_c.PolHoldS * fs);

            float dmin = _c.DistMm[0], dmax = _c.DistMm[0];
            for (int k = 1; k < _nch; k++)
            {
                if (_c.DistMm[k] < dmin) dmin = _c.DistMm[k];
                if (_c.DistMm[k] > dmax) dmax = _c.DistMm[k];
            }
            float split = 0.5f * (dmin + dmax);
            for (int k = 0; k < _nch; k++)
            {
                _isFar[k] = _c.DistMm[k] >= split;
                _is850[k] = _c.WlNm[k]  >  800f;
            }
            Reset();
        }

        static float Coef(float fs, float tau)
        {
            if (tau <= 0f || fs <= 0f) return 1f;
            float a = 1f - (float)Math.Exp(-1.0 / (fs * (double)tau));
            if (a > 1f) a = 1f;
            if (a < 0f) a = 0f;
            return a;
        }

        public void Reset()
        {
            for (int k = 0; k < MaxCh; k++)
            {
                _base[k] = 0f; _span[k] = _c.MinSpan; _noise[k] = 1e-5f;
                _prev[k] = 0f; _od[k] = 0f; _w[k] = 0f; _chOk[k] = 0; _rearm[k] = 0;
                _chReady[k] = 0; _chRun[k] = 0;
                _pol[k] = 1f; _polCov[k] = 0f; _polMdn[k] = 0f;
                _polVdn[k] = 0f; _polLock[k] = 0;
            }
            _n = 0; _stableProgress = 0f;
            _wRef = 0f; _health = 0f; _tracking = false;
            _mvcSettled = false; _mvcMark = 0f; _mvcMarkN = 0;
            _uLp = 0f; _level = 0f; _mvc = _c.MvcMin; _mvcLp = 0f;
            _spatial = 0f; _spectral = 0f; _uOd = 0f; _wsum = 0f;
            _polMref = 0f; _polVref = 0f; _farAct = 0f; _farG = 0f;
            _flags = NirsFlags.None; _out01 = 0f; _outV = 0f;
        }

        /// <summary>Одна выборка со всех каналов. Возвращает уровень 0..1.</summary>
        public float Update(float[] v)
        {
            bool first = _n == 0;
            float wsum = 0f, acc = 0f, spanRef = 0f, wReady = 0f;
            float farS = 0f, farW = 0f, nearS = 0f, nearW = 0f, farN = 0f;
            float s850 = 0f, w850 = 0f, s740 = 0f, w740 = 0f;
            NirsFlags fl = NirsFlags.None;

            _n++;

            for (int k = 0; k < _nch; k++)
            {
                float x = v[k];
                // Сравнения с NaN дают false, поэтому это же условие
                // отсеивает NaN, бесконечности и отрицательные значения.
                // Гистерезис: выпавший канал возвращается не на самой границе,
                // а на VHyst глубже. Канал, стоящий вплотную к пределу шкалы
                // ОУ, иначе входит и выходит из диапазона на каждой пульсовой
                // волне, а каждый возврат сбрасывает его базу.
                bool ok = _chOk[k] != 0
                    ? (x > _c.VMin && x < _c.VMax)
                    : (x > _c.VMin + _c.VHyst && x < _c.VMax - _c.VHyst);
                if (!ok)
                {
                    // Состояние непригодного канала НЕ обновляем: иначе один
                    // NaN или выброс навсегда отравил бы адаптивные переменные.
                    fl |= NirsFlags.Sat;
                    _od[k] = 0f;
                    _w[k]  = 0f;
                    // Канал выпал. Событие ЛОКАЛЬНОЕ: оно отнимает у общего
                    // здоровья установки долю этого канала и глобальных флагов
                    // само по себе не роняет. Снятый датчик роняет все каналы.
                    _w[k] = 0f;
                    _rearm[k]   = 0;
                    _chOk[k]    = 0;
                    _chReady[k] = 0;
                    _chRun[k]   = 0;
                    continue;
                }

                float a = -Log(x) * _pol[k];               // ослабление со знаком

                // База снимается «как есть», но если в этот момент остальные
                // каналы показывают сокращение, снятый уровень покоем не
                // является: канал ждёт покоя и в слиянии не участвует.
                if (first || _chOk[k] == 0)
                {
                    _base[k] = a; _prev[k] = a;
                    // Размах тоже сбрасывается: иначе вернувшийся канал получает
                    // полный вес (span уцелел) при нулевой активности (база только
                    // что снята) и разбавляет общий выход.
                    _span[k] = _c.MinSpan;
                    _rearm[k] = 1;   // возмущение засчитаем, когда канал снова
                                     // начнёт что-то весить
                    _chOk[k] = (byte)(_out01 <= _c.GateLo ? 2 : 1);
                }
                else if (_chOk[k] == 1 && _out01 <= _c.GateLo)
                {
                    _base[k] = a;
                    _chOk[k] = 2;
                }

                // базовая линия: быстро вверх, очень медленно вниз
                if (a > _base[k])
                {
                    _base[k] += _aBaseUp * (a - _base[k]);
                }
                else
                {
                    float sp = _span[k] < _c.MinSpan ? _c.MinSpan : _span[k];
                    float rr = (_base[k] - a) / sp;
                    float rate = _aBaseDn / (1f + _c.FreezeK * rr * rr);
                    // Плавный переход, а не порог: жёсткое переключение на
                    // границе дребезжит и делает результат чувствительным к
                    // последнему биту. _farG падает от 1 (дальние в покое)
                    // до ~1e-6 при сокращении, так что защита базы цела.
                    rate += _farG * (_aBaseUp - rate);
                    _base[k] += rate * (a - _base[k]);
                }

                float d = _base[k] - a;
                if (d < 0f) d = 0f;
                _od[k] = d;

                if (d > _span[k]) _span[k] += _aSpanUp * (d - _span[k]);
                else              _span[k] += _aSpanDn * (d - _span[k]);
                if (_span[k] < _c.MinSpan) _span[k] = _c.MinSpan;

                float diff = a - _prev[k];
                if (diff < 0f) diff = -diff;
                _noise[k] += _aNoise * (diff - _noise[k]);
                _prev[k] = a;
                float nz = _noise[k] < 1e-7f ? 1e-7f : _noise[k];

                float w;
                if (_chOk[k] == 2 && _span[k] > _c.MinSpan * 1.001f)
                {
                    float r = _span[k] / (_c.SnrK * nz);
                    w = (r * r) / (1f + r * r);
                }
                else
                {
                    w = 0f;
                    fl |= NirsFlags.LowSig;
                }
                if (w < _c.WMin) w = 0f;
                if (_rearm[k] != 0 && w > 0f && _span[k] > 2f * _c.MinSpan)
                    _rearm[k] = 0;          // канал доучился и снова в деле
                _w[k] = w;

                // готовность канала: непрерывно пригоден уже TrackS, прошёл
                // фазу ожидания покоя, набрал размах, не менял знак
                _chRun[k]++;
                _chReady[k] = (byte)((_chOk[k] == 2 && _rearm[k] == 0 &&
                                      _chRun[k] >= _trackN &&
                                      _span[k] > 2f * _c.MinSpan &&
                                      _n >= _polLock[k] && w > 0f) ? 1 : 0);
                if (_chReady[k] != 0) wReady += w;

                _dn[k] = 0f;
                if (w > 0f)
                {
                    _dn[k]   = d / _span[k];
                    acc     += w * _dn[k];
                    spanRef += w * _span[k];
                    wsum    += w;
                    if (_isFar[k]) { farS += w * d; farW += w; farN += w * _dn[k]; } else { nearS += w * d; nearW += w; }
                    if (_is850[k]) { s850 += w * d; w850 += w; } else { s740 += w * d; w740 += w; }
                }
            }

            _wsum = wsum;

            float u;
            if (wsum > 1e-6f) { spanRef /= wsum; u = (acc / wsum) * spanRef; }
            else              { u = 0f; fl |= NirsFlags.NoChan; }
            _uOd = u;

            // --- оценка полярности ближних каналов --------------------------
            // Опорный сигнал — нормированный вклад дальних каналов, их знак
            // принят за +1. Знак меняется только при уверенной ОТРИЦАТЕЛЬНОЙ
            // корреляции: нормировка на дисперсии обязательна, иначе слабый
            // шумный канал переключался бы туда-сюда.
            // согласие дальних каналов: признак покоя для базовой линии на
            // следующем шаге и опора для оценки полярности здесь
            _farAct = farW > 0f ? farN / farW : 0f;
            if (farW > 0f && _c.BaseRestThr > 0f)
            {
                float q = _farAct / _c.BaseRestThr;
                q *= q; q *= q; q *= q;              // (farAct/thr)^8
                _farG = 1f / (1f + q);
            }
            else _farG = 0f;                          // нет дальних каналов

            if (_c.AutoPolarity && farW > 0f)
            {
                float refn = _farAct;
                _polMref += _aPol * (refn - _polMref);
                float er = refn - _polMref;
                _polVref += _aPol * (er * er - _polVref);
                for (int k = 0; k < _nch; k++)
                {
                    if (_isFar[k] || _w[k] <= 0f) continue;
                    _polMdn[k] += _aPol * (_dn[k] - _polMdn[k]);
                    float ec = _dn[k] - _polMdn[k];
                    _polVdn[k] += _aPol * (ec * ec - _polVdn[k]);
                    _polCov[k] += _aPol * (ec * er - _polCov[k]);
                    float corr = _polCov[k] / (float)Math.Sqrt(_polVdn[k] * _polVref + 1e-20);
                    if (_polVref > 1e-6f && corr < -_c.PolThr && _n >= _polLock[k])
                    {
                        _pol[k]     = -_pol[k];
                        _polLock[k] = _n + _polHoldN;
                        _polCov[k]  = 0f; _polMdn[k] = 0f; _polVdn[k] = 0f;
                        _base[k]    = 0f;               // переучить базу и размах
                        _span[k]    = _c.MinSpan;
                        _od[k]      = 0f; _w[k] = 0f; _chOk[k] = 0;
                        _rearm[k]   = 1;                // знак сменился, база с нуля
                        _chReady[k] = 0;                // и канал временно не в счёт
                        fl |= NirsFlags.LowSig;
                    }
                }
            }

            _spatial  = (farW  > 0f ? farS  / farW  : 0f) - (nearW > 0f ? nearS / nearW : 0f);
            _spectral = (w850  > 0f ? s850  / w850  : 0f) - (w740  > 0f ? s740  / w740  : 0f);

            if (_leadGain > 0f)
            {
                _uLp += _aLead * (u - _uLp);
                u += _leadGain * (u - _uLp);
            }

            float aSm = u > _level ? _aAtt : _aRel;
            _level += aSm * (u - _level);

            // Шкалу задаёт устойчивый уровень, а не выброс на фронте.
            _mvcLp += _aMvcLp * (_level - _mvcLp);
            if (_mvcLp > _mvc) _mvc += _aMvcUp * (_mvcLp - _mvc);
            else               _mvc += _aMvcDn * (_mvcLp - _mvc);
            if (_mvc < _c.MvcMin) _mvc = _c.MvcMin;

            float o = _level / _mvc;
            if (_c.Deadzone > 0f && _c.Deadzone < 1f)
                o = (o - _c.Deadzone) / (1f - _c.Deadzone);
            if (o < 0f) o = 0f;
            // Мягкое ограничение: 0..1 -> 0..knee линейно, выше — сжатие
            // функцией u/(1+u) (монотонна, ограничена, дёшева).
            float kn = _c.SoftKnee;
            if (kn >= 1f)
            {
                if (o > 1f) { o = 1f; fl |= NirsFlags.Clipped; }
            }
            else if (o > 1f)
            {
                float ex = o - 1f;
                o = kn + (1f - kn) * (ex / (1f + ex));
                fl |= NirsFlags.Clipped;
            }
            else o *= kn;
            _out01 = o;
            _outV  = o * _c.OutVmax;

            if ((_flags & NirsFlags.Active) != 0)
            {
                if (o < _c.GateLo) _flags &= ~NirsFlags.Active;
            }
            else
            {
                if (o > _c.GateHi) _flags |= NirsFlags.Active;
            }

            fl |= _flags & NirsFlags.Active;
            if (_n >= _warmupN) fl |= NirsFlags.Ready;

            // Tracking: здоровье = вес готовых каналов / «сколько их тут обычно».
            // Опорная сумма помнит норму для этой установки, поэтому выпад
            // одного канала порога не пробивает, а снятие датчика роняет всё.
            if (wsum > _wRef) _wRef += _aBaseUp * (wsum - _wRef);
            else              _wRef += _aWRef   * (wsum - _wRef);

            _health = _wRef > 1e-6f ? wReady / _wRef : 0f;
            if (_health > 1f) _health = 1f;

            if (_tracking) { if (_health < _c.HealthLo) _tracking = false; }
            else           { if (_health > _c.HealthHi) _tracking = true;  }

            if (_tracking && (fl & NirsFlags.Ready) != 0 && (fl & NirsFlags.NoChan) == 0)
                fl |= NirsFlags.Tracking;

            // Stable: устоялся ещё и масштаб — за окно MvcSettleS шкала
            // сдвинулась меньше чем на MvcSettleThr и поднята настоящим
            // сокращением, а не сидит на нижнем пределе.
            if (_n - _mvcMarkN >= _mvcSettleN)
            {
                float rel = _mvc > 1e-6f ? (_mvc - _mvcMark) / _mvc : 1f;
                if (rel < 0f) rel = -rel;
                _mvcSettled = rel < _c.MvcSettleThr && _mvc > _c.MvcMin * 1.2f;
                _mvcMark = _mvc; _mvcMarkN = _n;
            }
            if (!_tracking) _mvcSettled = false;
            if ((fl & NirsFlags.Tracking) != 0 && _mvcSettled) fl |= NirsFlags.Stable;

            if ((fl & NirsFlags.Stable) != 0) _stableProgress = 1f;
            else if ((fl & NirsFlags.Tracking) != 0)
            {
                float q = _mvcSettleN > 0u ? (float)(_n - _mvcMarkN) / _mvcSettleN : 1f;
                if (q > 1f) q = 1f;
                _stableProgress = 0.5f + 0.5f * q;
            }
            else _stableProgress = 0.5f * _health;

            _flags = fl;

            return o;
        }

        /// <summary>Зафиксировать текущий уровень как 100 % шкалы.</summary>
        public void CalibrateMvc()
        {
            // берём сглаженный уровень: калибровка не должна попасть на выброс
            float v = _mvcLp > _level ? _mvcLp : _level;
            _mvc = v > _c.MvcMin ? v : _c.MvcMin;
        }

        /// <summary>Задать MVC явно (единицы OD).</summary>
        public void SetMvc(float mvcOd)
        {
            _mvc   = mvcOd > _c.MvcMin ? mvcOd : _c.MvcMin;
            _mvcLp = _mvc;
        }
    }
}
