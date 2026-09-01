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
        Clipped = 0x0020    // выход упёрся в 1.0
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

        public float TauBaseUp = 0.75f;
        public float TauBaseDn = 120f;
        public float FreezeK   = 4f;

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

        uint  _n;
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
        public float[]   Od       => _od;
        public float[]   W        => _w;
        public float[]   Span     => _span;
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
            _warmupN = (uint)(_c.WarmupS * fs);

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
                _prev[k] = 0f; _od[k] = 0f; _w[k] = 0f; _chOk[k] = 0;
            }
            _n = 0; _uLp = 0f; _level = 0f; _mvc = _c.MvcMin; _mvcLp = 0f;
            _spatial = 0f; _spectral = 0f; _uOd = 0f; _wsum = 0f;
            _flags = NirsFlags.None; _out01 = 0f; _outV = 0f;
        }

        /// <summary>Одна выборка со всех каналов. Возвращает уровень 0..1.</summary>
        public float Update(float[] v)
        {
            bool first = _n == 0;
            float wsum = 0f, acc = 0f, spanRef = 0f;
            float farS = 0f, farW = 0f, nearS = 0f, nearW = 0f;
            float s850 = 0f, w850 = 0f, s740 = 0f, w740 = 0f;
            NirsFlags fl = NirsFlags.None;

            _n++;

            for (int k = 0; k < _nch; k++)
            {
                float x = v[k];
                // Сравнения с NaN дают false, поэтому это же условие
                // отсеивает NaN, бесконечности и отрицательные значения.
                bool ok = x > _c.VMin && x < _c.VMax;
                if (!ok)
                {
                    // Состояние непригодного канала НЕ обновляем: иначе один
                    // NaN или выброс навсегда отравил бы адаптивные переменные.
                    fl |= NirsFlags.Sat;
                    _od[k] = 0f;
                    _w[k]  = 0f;
                    _chOk[k] = 0;
                    continue;
                }

                float a = -(float)Math.Log(x);

                // База снимается «как есть», но если в этот момент остальные
                // каналы показывают сокращение, снятый уровень покоем не
                // является: канал ждёт покоя и в слиянии не участвует.
                if (first || _chOk[k] == 0)
                {
                    _base[k] = a; _prev[k] = a;
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
                    _base[k] += (_aBaseDn / (1f + _c.FreezeK * rr * rr)) * (a - _base[k]);
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
                _w[k] = w;

                if (w > 0f)
                {
                    acc     += w * (d / _span[k]);
                    spanRef += w * _span[k];
                    wsum    += w;
                    if (_isFar[k]) { farS += w * d; farW += w; } else { nearS += w * d; nearW += w; }
                    if (_is850[k]) { s850 += w * d; w850 += w; } else { s740 += w * d; w740 += w; }
                }
            }

            _wsum = wsum;

            float u;
            if (wsum > 1e-6f) { spanRef /= wsum; u = (acc / wsum) * spanRef; }
            else              { u = 0f; fl |= NirsFlags.NoChan; }
            _uOd = u;

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
