// ============================================================================
//  NirsPrefilter.cs — предфильтр для нейросети, версия на C#.
//
//  Порт nirs_prefilter.py / nirs_nn_runtime.c. Три реализации одного и того
//  же: Python — для обучения, C — для прошивки, C# — для проверки на ПК.
//  Правите одну — правьте все три, иначе модель увидит разные числа.
//
//  Что делает: берёт на себя медленную адаптацию (логарифм, уровень покоя,
//  масштаб канала), которую рекуррентная сеть не может выучить из коротких
//  записей — постоянные времени доходят до 120 с, это 120 000 шагов при
//  1 кГц. Сети остаётся быстрая нелинейная часть.
//
//  Выход: 8 нормированных значений, примерно 0…1.2. Ноль в покое.
// ============================================================================
using System;

namespace NIRS_Demonstrator
{
    public sealed class NirsPrefilter
    {
        public const int MaxCh = 8;

        readonly float _aBaseUp, _aBaseDn, _aSpanUp, _aSpanDn;
        readonly float _vMin, _vMax, _minSpan, _freezeK;

        readonly float[] _base = new float[MaxCh];
        readonly float[] _span = new float[MaxCh];
        readonly bool[]  _ok   = new bool[MaxCh];

        public NirsPrefilter(float fs = 1000f,
                             float vMin = 0.02f, float vMax = 4.30f,
                             float tauBaseUp = 0.75f, float tauBaseDn = 120f,
                             float freezeK = 4f,
                             float tauSpanUp = 2f, float tauSpanDn = 60f,
                             float minSpan = 0.010f)
        {
            _aBaseUp = Coef(fs, tauBaseUp);
            _aBaseDn = Coef(fs, tauBaseDn);
            _aSpanUp = Coef(fs, tauSpanUp);
            _aSpanDn = Coef(fs, tauSpanDn);
            _vMin = vMin; _vMax = vMax; _minSpan = minSpan; _freezeK = freezeK;
            Reset();
        }

        static float Coef(float fs, float tau)
        {
            if (tau <= 0f || fs <= 0f) return 1f;
            return 1f - (float)Math.Exp(-1.0 / (fs * (double)tau));
        }

        public void Reset()
        {
            for (int k = 0; k < MaxCh; k++)
            {
                _base[k] = 0f; _span[k] = _minSpan; _ok[k] = false;
            }
        }

        /// <summary>Один шаг: 8 напряжений -> 8 нормированных значений.
        /// Вызывать на ПОЛНОЙ частоте дискретизации, строго по порядку.</summary>
        public void Step(float[] v, float[] outFeat)
        {
            for (int k = 0; k < MaxCh; k++)
            {
                float x = v[k];

                // сравнения с NaN дают false — непригодные каналы отсеиваются здесь
                if (!(x > _vMin && x < _vMax))
                {
                    _ok[k] = false;
                    outFeat[k] = 0f;
                    continue;
                }

                float a = -(float)Math.Log(x);

                if (!_ok[k]) { _base[k] = a; _ok[k] = true; }

                if (a > _base[k])
                {
                    _base[k] += _aBaseUp * (a - _base[k]);
                }
                else
                {
                    float sp = _span[k] < _minSpan ? _minSpan : _span[k];
                    float r = (_base[k] - a) / sp;
                    _base[k] += (_aBaseDn / (1f + _freezeK * r * r)) * (a - _base[k]);
                }

                float d = _base[k] - a;
                if (d < 0f) d = 0f;

                if (d > _span[k]) _span[k] += _aSpanUp * (d - _span[k]);
                else              _span[k] += _aSpanDn * (d - _span[k]);
                if (_span[k] < _minSpan) _span[k] = _minSpan;

                outFeat[k] = d / _span[k];
            }
        }

        /// <summary>Прогон массива целиком. Порядок строк важен.</summary>
        public float[][] Run(float[][] X)
        {
            var outp = new float[X.Length][];
            for (int i = 0; i < X.Length; i++)
            {
                outp[i] = new float[MaxCh];
                Step(X[i], outp[i]);
            }
            return outp;
        }
    }
}
