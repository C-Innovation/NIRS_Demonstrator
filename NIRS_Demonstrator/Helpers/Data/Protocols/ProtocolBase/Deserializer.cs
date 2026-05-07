using System;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// Извлекает и валидирует пакеты из кольцевого буфера.
    /// При успешной валидации вызывает callback с полезной нагрузкой.
    /// </summary>
    public class Deserializer
    {
        private readonly CircularBuffer<byte> _buffer;
        private readonly uint _expectedHeader;
        private readonly Action<byte[], int> _callback;
        private const int MaxPacketSize = 1024;

        public Deserializer(CircularBuffer<byte> buffer, uint expectedHeader, Action<byte[], int> callback)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _expectedHeader = expectedHeader;
            _callback = callback;
        }

        public void Process()
        {
            // Минимальный размер заголовка: 4 (header) + 2 (len) + 2 (crc) = 8 байт
            while (_buffer.Size >= 8)
            {
                int headerPos = FindHeader();
                if (headerPos == -1)
                {
                    // Заголовок не найден. Удаляем всё, кроме последних 3 байт 
                    // (на случай, если начало заголовка оказалось на границе).
                    int avail = _buffer.Size;
                    if (avail > 3)
                        Consume(avail - 3);
                    break;
                }

                // Удаляем мусор до заголовка
                if (headerPos > 0)
                    Consume(headerPos);

                // Читаем длину поля данных (Little-Endian)
                ushort dataLen = (ushort)(_buffer[4] | (_buffer[5] << 8));
                int totalPacketLen = 4 + 2 + dataLen + 2;

                // Проверка на адекватный размер
                if (dataLen > MaxPacketSize)
                {
                    Consume(1);
                    continue;
                }

                if (_buffer.Size < totalPacketLen)
                    break; // Пакет ещё не пришёл полностью

                // Вычисляем и сверяем CRC
                ushort calcCrc = ComputeCrc16FromBuffer(totalPacketLen - 2);
                ushort recvCrc = (ushort)(_buffer[totalPacketLen - 2] | (_buffer[totalPacketLen - 1] << 8));

                if (calcCrc != recvCrc)
                {
                    System.Diagnostics.Debug.WriteLine("[Deserializer] Invalid packet. CRC mismatch.");
                    Consume(1);
                    continue;
                }

                // Извлекаем payload
                // В оригинале использовался стек-массив на 256 байт. Сохраняем ограничение для совместимости.
                if (dataLen > 256)
                {
                    Consume(1);
                    continue;
                }

                byte[] payload = new byte[dataLen];
                for (int i = 0; i < dataLen; i++)
                    payload[i] = _buffer[6 + i];

                // Уведомляем потребителя
                _callback?.Invoke(payload, dataLen);

                // Удаляем весь пакет из буфера
                Consume(totalPacketLen);
            }
        }

        private int FindHeader()
        {
            int avail = _buffer.Size;
            if (avail < 4) return -1;

            for (int offset = 0; offset <= avail - 4; offset++)
            {
                uint val = (uint)(_buffer[offset] |
                                  (_buffer[offset + 1] << 8) |
                                  (_buffer[offset + 2] << 16) |
                                  (_buffer[offset + 3] << 24));
                if (val == _expectedHeader)
                    return offset;
            }
            return -1;
        }

        private ushort ComputeCrc16FromBuffer(int len)
        {
            ushort crc = 0x0000;
            for (int i = 0; i < len; i++)
            {
                crc ^= (ushort)(_buffer[i] << 8);
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

        /// <summary>
        /// Удаляет count элементов с начала буфера.
        /// (В оригинальном CircularBuffer нет пакетного удаления, поэтому цикл)
        /// </summary>
        private void Consume(int count)
        {
            for (int i = 0; i < count; i++)
                _buffer.PopFront();
        }
    }
}
