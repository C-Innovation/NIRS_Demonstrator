// ============================================================================
//  NirsLeveler.cs — приведение уровней покоя сырых каналов к заданным.
//
//  Точный порт nirs_leveler.c. Держите оба файла синхронными.
//
//  Зачем. Абсолютный уровень зависит от установки датчика, кожи и тока
//  светодиодов: на трёх записях уровень покоя канала 740_26 равен
//  0.447 / 0.262 / 0.210 В — разброс в 2.1 раза. После приведения к 1 В
//  разброс между сессиями падает до 8 %.
//
//  Формат: Process(float[8] in, float[8] out). Допускается in == out.
//
//  --- Коррекция МУЛЬТИПЛИКАТИВНАЯ: out = in * (target / rest) ---
//  Сокращение меняет пропускание ткани в разы, а не на вольты: отношение
//  пик/покой держится в пределах 1.69…1.75 при уровнях покоя 0.21…0.45 В,
//  а разность пик−покой гуляет от 0.14 до 0.34 В. Умножение оставляет
//  разброс между сессиями 11 %, вычитание — 22 %. Аддитивный режим есть
//  (Additive), но по умолчанию выключен.
//
//  --- Два опорных уровня, и это важно ---
//  _ref  — нижняя огибающая, вниз быстро / вверх медленно, НИКОГДА не
//          замораживается. По ней работает детектор активности.
//  _rest — уровень покоя для коэффициента, замораживается на сокращении.
//
//  Разделять обязательно. Симметричный трекер для детектора не годится:
//  сокращения занимают около половины записи, трекер садится посередине,
//  и относительное отклонение в покое выходит таким же, как при сокращении
//  (измерено: 0.19…0.26 против 0.16…0.20) — детектор залипает в «активно»
//  на 97…100 % времени и адаптация не идёт вообще. С нижней огибающей
//  разделение чистое: покой до 0.037, сокращение от 0.24.
// ============================================================================
using System;

namespace NIRS_Demonstrator
{
    [Flags]
    public enum NirsLvlFlags : ushort
    {
        None    = 0,
        Ready   = 0x0001,   // уровни найдены, выход достоверен
        Active  = 0x0002,   // идёт сокращение, адаптация заморожена
        Invalid = 0x0004,   // канал вне окна пригодности
        Clamped = 0x0008,   // коэффициент упёрся в предел
        Dead    = 0x0010    // канал без модуляции, отдан как есть
    }

    public sealed class NirsLevelerConfig
    {
        public const int MaxCh = 8;

        public float Fs = 1000f;

        /// <summary>Желаемый уровень покоя каждого канала, В.</summary>
        public float[] Target = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        public float VMin = 0.02f;
        /// <summary>Ставьте ЗАМЕТНО ниже реального предела ОУ. Ближние каналы
        /// стоят на 4.23…4.39 В; при VMax = 4.30 канал 740_10mm (4.233 В)
        /// прошёл бы проверку и был бы поднят до 1 В, то есть стал бы
        /// неотличим от рабочего.</summary>
        public float VMax = 4.00f;

        public float TauRefDn = 0.5f;    // нижняя огибающая: спуск
        public float TauRefUp = 20f;     // ... и подъём

        public float TauRest  = 1f;
        public float TauGain  = 1f;
        /// <summary>Первые ConvergeS секунд коэффициент идёт прямо к
        /// Target/ref без сглаживания и предела скорости: иначе при 20 %
        /// времени покоя и пределе 10 %/с он выходит на режим десятки
        /// секунд.</summary>
        public float ConvergeS = 5f;

        public float GainMin  = 0.05f;
        public float GainMax  = 20f;
        public float GainSlew = 0.10f;   // макс. относительное изменение, 1/с

        // Детектор: покой не выше 0.037, сокращение не ниже 0.24 -> порог 0.08
        public float ThrDev  = 0.08f;
        public float TauFast = 0.008f;
        public float TauSlow = 0.080f;
        public float ThrEdge = 0.12f;
        public float HoldS   = 0.5f;

        // Защита от «оживления» мёртвого канала
        public float MinSpanRel = 0.02f;
        public float SpanGraceS = 10f;

        public bool GlobalFreeze       = true;
        public bool PassthroughInvalid = true;
        public bool Additive           = false;
        public float WarmupS = 2f;

        public NirsLevelerConfig Clone()
        {
            var c = (NirsLevelerConfig)MemberwiseClone();
            c.Target = (float[])Target.Clone();
            return c;
        }
    }

    public sealed class NirsLeveler
    {
        public const int MaxCh = NirsLevelerConfig.MaxCh;

        readonly NirsLevelerConfig _c;
        readonly float _aRefDn, _aRefUp, _aRest, _aGain, _aFast, _aSlow;
        readonly float _aSpanUp, _aSpanDn, _slewStep;
        readonly uint _warmupN, _holdN, _graceN, _convergeN;

        readonly float[] _ref  = new float[MaxCh];
        readonly float[] _rest = new float[MaxCh];
        readonly float[] _gain = new float[MaxCh];
        readonly float[] _fast = new float[MaxCh];
        readonly float[] _slow = new float[MaxCh];
        readonly float[] _span = new float[MaxCh];
        readonly bool[]  _ok   = new bool[MaxCh];
        readonly bool[]  _act  = new bool[MaxCh];
        readonly bool[]  _dead = new bool[MaxCh];
        readonly uint[]  _hold = new uint[MaxCh];
        readonly float[] _x    = new float[MaxCh];

        uint _n;
        NirsLvlFlags _flags;

        public NirsLvlFlags Flags  => _flags;
        public bool  Active        => (_flags & NirsLvlFlags.Active) != 0;
        public bool  Ready         => (_flags & NirsLvlFlags.Ready)  != 0;
        public float[] Gain        => _gain;
        public float[] Rest        => _rest;
        public NirsLevelerConfig Config => _c;

        /// <summary>Канал приводится к уровню (не насыщен и не мёртв).</summary>
        public bool ChannelOk(int ch) => ch >= 0 && ch < MaxCh && _ok[ch] && !_dead[ch];

        public NirsLeveler(NirsLevelerConfig cfg = null)
        {
            _c = (cfg ?? new NirsLevelerConfig()).Clone();
            float fs = _c.Fs > 0f ? _c.Fs : 1000f;

            _aRefDn  = Coef(fs, _c.TauRefDn);
            _aRefUp  = Coef(fs, _c.TauRefUp);
            _aRest   = Coef(fs, _c.TauRest);
            _aGain   = Coef(fs, _c.TauGain);
            _aFast   = Coef(fs, _c.TauFast);
            _aSlow   = Coef(fs, _c.TauSlow);
            _aSpanUp = Coef(fs, 2f);
            _aSpanDn = Coef(fs, 60f);

            _slewStep  = _c.GainSlew > 0f ? _c.GainSlew / fs : 1e9f;
            _warmupN   = (uint)(_c.WarmupS    * fs);
            _holdN     = (uint)(_c.HoldS      * fs);
            _graceN    = (uint)(_c.SpanGraceS * fs);
            _convergeN = (uint)(_c.ConvergeS  * fs);

            Reset();
        }

        /// <summary>Все каналы к одному уровню.</summary>
        public NirsLeveler(float targetVolts, float fs = 1000f)
            : this(MakeCfg(targetVolts, fs)) { }

        static NirsLevelerConfig MakeCfg(float t, float fs)
        {
            var c = new NirsLevelerConfig { Fs = fs };
            for (int k = 0; k < NirsLevelerConfig.MaxCh; k++) c.Target[k] = t;
            return c;
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
                _ref[k] = 0f; _rest[k] = 0f; _gain[k] = 1f;
                _fast[k] = 0f; _slow[k] = 0f; _span[k] = 0f;
                _ok[k] = false; _act[k] = false; _dead[k] = false; _hold[k] = 0u;
            }
            _n = 0; _flags = NirsLvlFlags.None;
        }

        /// <summary>Сменить целевой уровень канала на ходу.</summary>
        public void SetTarget(int ch, float volts)
        {
            if (ch < 0 || ch >= MaxCh || !(volts > 0f)) return;
            _c.Target[ch] = volts;   // коэффициент подтянется сам
        }

        /// <summary>ОСНОВНОЙ МЕТОД: 8 вольт на входе -> 8 вольт на выходе.
        /// Допускается in == out. Вызывать строго с частотой Fs.</summary>
        public void Process(float[] input, float[] output)
        {
            bool anyActive = false;
            NirsLvlFlags fl = NirsLvlFlags.None;
            _n++;

            for (int k = 0; k < MaxCh; k++)
            {
                float v = input[k];
                _x[k] = v;                                  // in может == out

                // сравнение с NaN даёт false -> NaN отсеивается здесь же
                if (!(v > _c.VMin && v < _c.VMax))
                {
                    _ok[k] = false; _act[k] = false;
                    fl |= NirsLvlFlags.Invalid;
                    continue;
                }

                if (!_ok[k])
                {
                    _ref[k] = v; _rest[k] = v; _fast[k] = v; _slow[k] = v;
                    _span[k] = 0f; _gain[k] = _c.Target[k] / v; _ok[k] = true;
                }

                // нижняя огибающая — опора детектора, не морозится
                _ref[k] += (v < _ref[k] ? _aRefDn : _aRefUp) * (v - _ref[k]);
                if (_ref[k] < 1e-6f) _ref[k] = 1e-6f;

                _fast[k] += _aFast * (v - _fast[k]);
                _slow[k] += _aSlow * (v - _slow[k]);

                float r = _ref[k];
                float dev = (v - r) / r;
                float edge = Math.Abs((_fast[k] - _slow[k]) / r);
                _act[k] = dev > _c.ThrDev || edge > _c.ThrEdge;
                if (_act[k]) anyActive = true;

                float d = dev < 0f ? 0f : dev;
                _span[k] += (d > _span[k] ? _aSpanUp : _aSpanDn) * (d - _span[k]);
                _dead[k] = _n > _graceN && _span[k] < _c.MinSpanRel;
                if (_dead[k]) fl |= NirsLvlFlags.Dead;
            }

            // удержание — счётчик на канал; при GlobalFreeze активность
            // любого канала перезаряжает счётчики всех
            for (int k = 0; k < MaxCh; k++)
            {
                if (_act[k] || (_c.GlobalFreeze && anyActive)) _hold[k] = _holdN;
                else if (_hold[k] > 0) _hold[k]--;
                if (_hold[k] > 0) fl |= NirsLvlFlags.Active;
            }

            bool converging = _n <= _convergeN;
            for (int k = 0; k < MaxCh; k++)
            {
                if (!_ok[k] || _dead[k]) continue;

                if (converging)
                {
                    // опираемся на нижнюю огибающую: она уже нашла уровень
                    // покоя за доли секунды и по построению не поднимается
                    // за сокращением, поэтому заморозка здесь не нужна
                    _rest[k] = _ref[k];
                }
                else
                {
                    if (_hold[k] != 0) continue;             // заморожено
                    _rest[k] += _aRest * (_x[k] - _rest[k]);
                }
                if (_rest[k] < 1e-6f) _rest[k] = 1e-6f;

                float g = _c.Target[k] / _rest[k];
                if (g < _c.GainMin) { g = _c.GainMin; fl |= NirsLvlFlags.Clamped; }
                if (g > _c.GainMax) { g = _c.GainMax; fl |= NirsLvlFlags.Clamped; }

                if (converging) _gain[k] = g;
                else
                {
                    float d = _aGain * (g - _gain[k]);
                    float lim = _slewStep * _gain[k];        // предел относительный
                    if (d >  lim) d =  lim;
                    if (d < -lim) d = -lim;
                    _gain[k] += d;
                }
            }

            for (int k = 0; k < MaxCh; k++)
            {
                if (!_ok[k])       output[k] = _c.PassthroughInvalid ? _x[k] : _c.Target[k];
                else if (_dead[k]) output[k] = _x[k];        // мёртвый не трогаем
                else if (_c.Additive) output[k] = _x[k] - _rest[k] + _c.Target[k];
                else                  output[k] = _x[k] * _gain[k];
            }

            if (_n >= _warmupN) fl |= NirsLvlFlags.Ready;
            _flags = fl;
        }

        /// <summary>Удобная форма: возвращает новый массив.</summary>
        public float[] Process(float[] input)
        {
            var o = new float[MaxCh];
            Process(input, o);
            return o;
        }
    }
}

// ============================================================================
//  ПРИМЕР
// ============================================================================
#if NIRS_LEVELER_DEMO
static class LevelerDemo
{
    public static void Run()
    {
        // все каналы к 1.0 В
        var lvl = new Nirs.NirsLeveler(targetVolts: 1.0f, fs: 1000f);

        // либо каждому свой уровень
        var cfg = new Nirs.NirsLevelerConfig { Fs = 1000f };
        cfg.Target[2] = 1.0f;   // 740_26
        cfg.Target[3] = 0.8f;   // 740_32 — чуть ниже, чтобы сохранить порядок
        var lvl2 = new Nirs.NirsLeveler(cfg);

        var core = new Nirs.NirsContraction(new Nirs.NirsConfig { Fs = 1000f });
        var raw = new float[8];
        var lev = new float[8];

        while (ReadFrame(raw))
        {
            lvl.Process(raw, lev);           // 8 float -> 8 float

            if (!lvl.Ready) continue;        // первые 2 с уровни ещё ищутся

            float y = core.Update(lev);      // дальше по цепочке как обычно

            if (lvl.Active) { /* идёт сокращение, коэффициенты заморожены */ }
        }
        System.GC.KeepAlive(lvl2);
    }

    static bool ReadFrame(float[] v) => false;   // ваш источник данных
}
#endif
