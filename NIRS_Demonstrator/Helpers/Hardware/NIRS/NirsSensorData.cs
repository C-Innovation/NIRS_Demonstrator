using System;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    public struct NirsSensorData
    {
        public ulong Time;

        public ushort Led740_1;
        public ushort Led740_2;
        public ushort Led740_3;
        public ushort Led740_4;

        public ushort Led740_Bgd_1;
        public ushort Led740_Bgd_2;
        public ushort Led740_Bgd_3;
        public ushort Led740_Bgd_4;

        public ushort Led850_1;
        public ushort Led850_2;
        public ushort Led850_3;
        public ushort Led850_4;

        public ushort Led850_Bgd_1;
        public ushort Led850_Bgd_2;
        public ushort Led850_Bgd_3;
        public ushort Led850_Bgd_4;
    }

    public struct NirsSensorFilteredData
    {
        public ulong Time;

        public ushort Led740_1;
        public ushort Led740_2;
        public ushort Led740_3;
        public ushort Led740_4;

        public ushort Led850_1;
        public ushort Led850_2;
        public ushort Led850_3;
        public ushort Led850_4;

    }

    public static class SensorDataHelpers
    {
        public static NirsSensorData ToNirsSensorData(this byte[] rawData)
        {
            NirsSensorData data = new NirsSensorData();

            try
            {
                // byte[] tData = new byte[] {rawData[21], rawData[20], rawData[19], rawData[18] };
                data = new NirsSensorData()
                {
                    Time = BitConverter.ToUInt64(rawData, 0),
    

                    // Led740_1 = BitConverter.ToSingle(tData, 0),
                    Led740_1 = BitConverter.ToUInt16(rawData, 8),
                    Led740_2 = BitConverter.ToUInt16(rawData, 10),
                    Led740_3 = BitConverter.ToUInt16(rawData, 12),
                    Led740_4 = BitConverter.ToUInt16(rawData, 14),

                    Led740_Bgd_1 = BitConverter.ToUInt16(rawData, 16),
                    Led740_Bgd_2 = BitConverter.ToUInt16(rawData, 18),
                    Led740_Bgd_3 = BitConverter.ToUInt16(rawData, 20),
                    Led740_Bgd_4 = BitConverter.ToUInt16(rawData, 22),

                    Led850_1 = BitConverter.ToUInt16(rawData, 24),
                    Led850_2 = BitConverter.ToUInt16(rawData, 26),
                    Led850_3 = BitConverter.ToUInt16(rawData, 28),
                    Led850_4 = BitConverter.ToUInt16(rawData, 30),

                    Led850_Bgd_1 = BitConverter.ToUInt16(rawData, 32),
                    Led850_Bgd_2 = BitConverter.ToUInt16(rawData, 34),
                    Led850_Bgd_3 = BitConverter.ToUInt16(rawData, 36),
                    Led850_Bgd_4 = BitConverter.ToUInt16(rawData, 38),
                };
            }
            catch
            {

            }


            return data;
        }

        public static byte[] ToByteArray(this NirsSensorData data)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(data.Time));

            bytes.AddRange(BitConverter.GetBytes(data.Led740_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_4));

            bytes.AddRange(BitConverter.GetBytes(data.Led740_Bgd_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_Bgd_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_Bgd_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_Bgd_4));

            bytes.AddRange(BitConverter.GetBytes(data.Led850_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_4));

            bytes.AddRange(BitConverter.GetBytes(data.Led850_Bgd_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_Bgd_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_Bgd_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_Bgd_4));

            return bytes.ToArray();

        }

        public static NirsSensorFilteredData ToNirsSensorFilteredData(this byte[] rawData)
        {
            NirsSensorFilteredData data = new NirsSensorFilteredData();

            try
            {
                // byte[] tData = new byte[] {rawData[21], rawData[20], rawData[19], rawData[18] };
                data = new NirsSensorFilteredData()
                {
                    Time = BitConverter.ToUInt64(rawData, 0),

                    // Led740_1 = BitConverter.ToSingle(tData, 0),
                    Led740_1 = BitConverter.ToUInt16(rawData, 8),
                    Led740_2 = BitConverter.ToUInt16(rawData, 10),
                    Led740_3 = BitConverter.ToUInt16(rawData, 12),
                    Led740_4 = BitConverter.ToUInt16(rawData, 14),

                    Led850_1 = BitConverter.ToUInt16(rawData, 16),
                    Led850_2 = BitConverter.ToUInt16(rawData, 18),
                    Led850_3 = BitConverter.ToUInt16(rawData, 20),
                    Led850_4 = BitConverter.ToUInt16(rawData, 22),
                };
            }
            catch
            {

            }


            return data;
        }

        public static byte[] ToByteArray(this NirsSensorFilteredData data)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(data.Time));

            bytes.AddRange(BitConverter.GetBytes(data.Led740_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led740_4));

            bytes.AddRange(BitConverter.GetBytes(data.Led850_1));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_2));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_3));
            bytes.AddRange(BitConverter.GetBytes(data.Led850_4));

            return bytes.ToArray();

        }

        public static double ToVoltage5V(this UInt16 raw, UInt16 resolutoin)
        {
            return (double)raw / Math.Pow(2, resolutoin) * 5.0;
        }
    }
}
