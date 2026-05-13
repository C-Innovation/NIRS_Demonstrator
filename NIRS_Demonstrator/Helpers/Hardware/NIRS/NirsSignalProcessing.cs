using CInnovation.SignalProcessing.Filters.BiQuad;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class NirsSignalProcessing
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        SlipMid[] _SlipMids;
        LowpassFilter[] _LowpassFilters;
        SlipMidSmart[] _SlipMidsSmart;
        #endregion

        #region Public Properties

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public NirsSignalProcessing()
        {
            _LowpassFilters = new LowpassFilter[4]
            {
                new LowpassFilter(100, 10.0),
                new LowpassFilter(100, 10.0),
                new LowpassFilter(100, 10.0),
                new LowpassFilter(100, 10.0)
            };

            _SlipMids = new SlipMid[8]
            {
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10),
                new SlipMid(10)
            };

            _SlipMidsSmart = new SlipMidSmart[8]
            {
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1),
                new SlipMidSmart(100, 10000, 0.3, 0.1)
            };
        }

        ~NirsSignalProcessing()
        {
            _LowpassFilters = null;
            _SlipMids = null;
            _SlipMidsSmart = null;
        }
        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        
        public NirsSignalData GetNirsSignalData(NirsSensorData data)
        {
            NirsSignalData signalData = new NirsSignalData();
            signalData.Led740Ch1 = RemoveLedBackground(data.Led740_1, data.Led740_Bgd_1).ToVoltage5V(12);
            signalData.Led740Ch2 = RemoveLedBackground(data.Led740_2, data.Led740_Bgd_2).ToVoltage5V(12);
            signalData.Led740Ch3 = RemoveLedBackground(data.Led740_3, data.Led740_Bgd_3).ToVoltage5V(12);
            signalData.Led740Ch4 = RemoveLedBackground(data.Led740_4, data.Led740_Bgd_4).ToVoltage5V(12);

            signalData.Led850Ch1 = RemoveLedBackground(data.Led850_3, data.Led850_Bgd_3).ToVoltage5V(12);
            signalData.Led850Ch2 = RemoveLedBackground(data.Led850_4, data.Led850_Bgd_4).ToVoltage5V(12);
            signalData.Led850Ch3 = RemoveLedBackground(data.Led850_3, data.Led850_Bgd_3).ToVoltage5V(12);
            signalData.Led850Ch4 = RemoveLedBackground(data.Led850_4, data.Led850_Bgd_4).ToVoltage5V(12);

            signalData.Led740Ch1_Flt = _SlipMids[0].Process(signalData.Led740Ch1);
            signalData.Led740Ch1_Flt = _SlipMidsSmart[0].Process(signalData.Led740Ch1_Flt);

            signalData.Led740Ch2_Flt = _SlipMids[1].Process(signalData.Led740Ch2);
            signalData.Led740Ch2_Flt = _SlipMidsSmart[1].Process(signalData.Led740Ch2_Flt);

            signalData.Led740Ch3_Flt = _SlipMids[2].Process(signalData.Led740Ch3);
            signalData.Led740Ch3_Flt = _SlipMidsSmart[2].Process(signalData.Led740Ch3_Flt);

            signalData.Led740Ch4_Flt = _SlipMids[3].Process(signalData.Led740Ch4);
            signalData.Led740Ch4_Flt = _SlipMidsSmart[3].Process(signalData.Led740Ch4_Flt);

            signalData.Led850Ch1_Flt = _SlipMids[4].Process(signalData.Led850Ch1);
            signalData.Led850Ch1_Flt = _SlipMidsSmart[4].Process(signalData.Led850Ch1_Flt);

            signalData.Led850Ch2_Flt = _SlipMids[5].Process(signalData.Led850Ch2);
            signalData.Led850Ch2_Flt = _SlipMidsSmart[5].Process(signalData.Led850Ch2_Flt);

            signalData.Led850Ch3_Flt = _SlipMids[6].Process(signalData.Led850Ch3);
            signalData.Led850Ch3_Flt = _SlipMidsSmart[6].Process(signalData.Led850Ch3_Flt);

            signalData.Led850Ch4_Flt = _SlipMids[7].Process(signalData.Led850Ch4);
            signalData.Led850Ch4_Flt = _SlipMidsSmart[7].Process(signalData.Led850Ch4_Flt);

            return signalData;
        }

        #endregion

        #region Private Methods

        ushort RemoveLedBackground(ushort val, ushort bgd)
        {
            if (val < bgd)
                return 0;

            return (ushort)(val - bgd);
        }

        #endregion
    }

    public struct NirsSignalData
    {
        public double Led740Ch1;
        public double Led740Ch2;
        public double Led740Ch3;
        public double Led740Ch4;

        public double Led740Ch1_Flt;
        public double Led740Ch2_Flt;
        public double Led740Ch3_Flt;
        public double Led740Ch4_Flt;

        public double Led850Ch1;
        public double Led850Ch2;
        public double Led850Ch3;
        public double Led850Ch4;
                             
        public double Led850Ch1_Flt;
        public double Led850Ch2_Flt;
        public double Led850Ch3_Flt;
        public double Led850Ch4_Flt;

        public List<double> ToList()
        {
            List<double> vals = new List<double>();
            vals.Add(Led740Ch1);
            vals.Add(Led740Ch2);
            vals.Add(Led740Ch3);
            vals.Add(Led740Ch4);

            vals.Add(Led740Ch1_Flt);
            vals.Add(Led740Ch2_Flt);
            vals.Add(Led740Ch3_Flt);
            vals.Add(Led740Ch4_Flt);

            vals.Add(Led850Ch1);
            vals.Add(Led850Ch2);
            vals.Add(Led850Ch3);
            vals.Add(Led850Ch4);

            vals.Add(Led850Ch1_Flt);
            vals.Add(Led850Ch2_Flt);
            vals.Add(Led850Ch3_Flt);
            vals.Add(Led850Ch4_Flt);

            return vals;    
        }
    }
}
