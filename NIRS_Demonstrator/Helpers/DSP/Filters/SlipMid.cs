using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class SlipMid
    {
        private readonly int _WindowSize;
        private readonly Queue<double> _Samples;

        public int WindowSize => _WindowSize;
        public SlipMid(int windowSize)
        {
            _WindowSize = windowSize;
            _Samples = new Queue<double>(_WindowSize);
        }

        public double Process(double val)
        {
            double result = 0;
            _Samples.Enqueue(val);

            if (_Samples.Count > _WindowSize)
                _Samples.Dequeue();

            foreach (var sample in _Samples)
                result += sample;
            return result / _Samples.Count;

        }
    }
}
