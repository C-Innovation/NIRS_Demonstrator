using System;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Упаковывает данные в бинарный пакет: [Header(4)] [Length(2)] [Data] [CRC16(2)]
    /// </summary>
    public class Serializer
    {
        private readonly uint _header;

        public Serializer(uint header) => _header = header;

        /// <summary>
        /// Сериализует данные в выходной буфер.
        /// </summary>
        /// <param name="data">Полезная нагрузка</param>
        /// <param name="outBuffer">Целевой буфер</param>
        /// <returns>Длина пакета или 0, если outBuffer слишком мал</returns>
        public int Serialize(ReadOnlySpan<byte> data, Span<byte> outBuffer)
        {
            int packetLen = 4 + 2 + data.Length + 2;
            if (outBuffer.Length < packetLen) return 0;

            // Заголовок (Little-Endian)
            outBuffer[0] = (byte)(_header);
            outBuffer[1] = (byte)(_header >> 8);
            outBuffer[2] = (byte)(_header >> 16);
            outBuffer[3] = (byte)(_header >> 24);

            // Длина данных (Little-Endian)
            outBuffer[4] = (byte)(data.Length);
            outBuffer[5] = (byte)(data.Length >> 8);

            // Данные
            data.CopyTo(outBuffer.Slice(6, data.Length));

            // Контрольная сумма (CRC-16/ARC)
            ushort checksum = ComputeCrc16(outBuffer.Slice(0, packetLen - 2));
            outBuffer[packetLen - 2] = (byte)(checksum);
            outBuffer[packetLen - 1] = (byte)(checksum >> 8);

            return packetLen;
        }

        private static ushort ComputeCrc16(ReadOnlySpan<byte> data)
        {
            ushort crc = 0x0000;
            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x8005);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }
    }
}
