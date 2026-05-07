using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    public struct ChartData
    {
        public byte Channel;
        public double X; 
        public double Y;

        public ChartData()
        {
            
        }

        public ChartData(byte channel, double x, double y)
        {
            Channel = channel;
            X = x;
            Y = y;
        }
    }
}
