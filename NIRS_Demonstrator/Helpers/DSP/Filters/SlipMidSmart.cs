using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class SlipMidSmart
    {
        #region Protected Members

        #endregion

        #region Private Members

        private const int TIMEOUT_START_MS = 10000;

        private SlipMidSmartState _CurrentState = SlipMidSmartState.Idle;
        private int _SampleRate;
        private int _TimeOutMS;
        private int _TimeOutSamples;
        private int _TimeOutStartSamples;
        private int _TimeOutCounter;
        private double _PosTrigVal;
        private double _NegTrigVal;

        private int _WindowSize;

        private double _LastVal = 0.0;
        private double _MidSum = 0.0;
        private double _MidVal = 0.0;
        private ulong _MidCounter = 0;
        private bool _MidCalcEn = true;

        private Queue<double> _Samples;
        #endregion

        #region Public Properties

        public bool MidCalcEn { get => _MidCalcEn; }
        public double CurrentPosLevel { get => (_MidVal + _PosTrigVal); }
        public double CurrentNegLevel { get => (_MidVal - _NegTrigVal); }
        public double CurrentMidLevel { get => _MidVal; }


        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public SlipMidSmart(int sampleRate, int windowsize, int timeOutMS, double posTrigVal, double negTrigVal)
        {
            _CurrentState = SlipMidSmartState.Idle;
            _WindowSize = windowsize;
            _SampleRate = sampleRate;
            _TimeOutMS = timeOutMS;
            _TimeOutSamples = (int)((double)timeOutMS / 1000.0) * _SampleRate;
            _TimeOutStartSamples = (int)((double)TIMEOUT_START_MS / 1000.0) * _SampleRate;
            _PosTrigVal = posTrigVal;
            _NegTrigVal = negTrigVal;
            _Samples = new Queue<double>(_WindowSize);
            _MidCalcEn = true;
        }

        #endregion

        #region Public Methods

        public double Process(double val)
        {
            if (_TimeOutStartSamples > 0)
            {
                _TimeOutStartSamples--;
                return ProcessMidCalc(val);
            }

            switch (_CurrentState)
            {
                case SlipMidSmartState.Idle:
                    if(CheckNegSet(val))
                    {
                        _CurrentState = SlipMidSmartState.AwaitForNegReset;
                        _MidCalcEn = false;
                        _TimeOutCounter = _TimeOutSamples;
                    }
                    else if (CheckPosSet(val))
                    {
                        _CurrentState = SlipMidSmartState.AwaitForPosReset;
                        _MidCalcEn = false;
                        _TimeOutCounter = _TimeOutSamples;
                    }
                    break;

                case SlipMidSmartState.AwaitForNegReset:
                    _TimeOutCounter--;
                    if(CheckNegReset(val) || _TimeOutCounter <= 0)
                    {
                        _CurrentState = SlipMidSmartState.Idle;
                        _MidCalcEn = true;
                    }
                    break;

                case SlipMidSmartState.AwaitForPosReset:
                    _TimeOutCounter--;
                    if (CheckPosReset(val) || _TimeOutCounter <= 0)
                    {
                        _CurrentState = SlipMidSmartState.Idle;
                        _MidCalcEn = true;
                    }
                    break;

                default: break;
            }
            return ProcessMidCalc(val);
        }

        public void Reset()
        {
            _CurrentState = SlipMidSmartState.Idle;
            _TimeOutStartSamples = (int)((double)TIMEOUT_START_MS / 1000.0) * _SampleRate;
            _MidCalcEn = true;
            _LastVal = 0.0;
            _MidSum = 0.0;
            _MidVal = 0.0;
            _MidCounter = 0;
        }

        #endregion

        #region Private Methods

        private bool CheckNegSet(double val)
        {
            if (_LastVal == 0.0)
            {
                _LastVal = val;
                return false;
            }

            if (_LastVal > (_MidVal - _NegTrigVal) && val <= (_MidVal - _NegTrigVal))
                return true;

            return false;
        }

        private bool CheckNegReset(double val)
        {
            if (_LastVal == 0.0) 
            {
                _LastVal = val;
                return false;
            }

            if(_LastVal < (_MidVal - _NegTrigVal) && val >= (_MidVal - _NegTrigVal))
                return true;

            return false;
        }

        private bool CheckPosSet(double val)
        {
            if (_LastVal == 0.0)
            {
                _LastVal = val;
                return false;
            }

            if (_LastVal < (_MidVal +_PosTrigVal) && val >= (_MidVal + _PosTrigVal))
                return true;

            return false;
        }

        private bool CheckPosReset(double val)
        {
            if (_LastVal == 0.0)
            {
                _LastVal = val;
                return false;
            }

            if (_LastVal > (_MidVal + _PosTrigVal) && val <= (_MidVal + _PosTrigVal))
                return true;

            return false;
        }

        private double ProcessMidCalc(double val)
        {
            if(_Samples.Count < _WindowSize)
            {
                _Samples.Enqueue(val);

                foreach (var sample in _Samples)
                    _MidSum += sample;

                _MidVal = _MidSum / _Samples.Count;

                return val - _MidVal;
            }

            if(_MidCalcEn)
            {
                _Samples.Enqueue(val);

                if (_Samples.Count > _WindowSize)
                {
                    _MidSum -= _Samples.ElementAt(0);
                    _MidSum += val;
                    _Samples.Dequeue();
                }

                //foreach (var sample in _Samples)
                //    _MidSum += sample;
                 
                _MidVal = _MidSum / _Samples.Count;


                //_MidSum += val;
                //_MidCounter++;
                //_MidVal = _MidSum / (double)_MidCounter;
            }
            return val - _MidVal;
        }

        #endregion
    }

    public enum SlipMidSmartState
    {
        Idle = 0,
        AwaitForNegReset = 1,
        AwaitForPosReset = 2,
    }

    public struct SlipMidSmartData
    {
        public bool MidCalcEn;
        public double CurrentPosLevel;
        public double CurrentNegLevel;
        public double CurrentMidLevel;
    }
}
