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
                ushort dataLen = (ushort)(_buffer.GetUnchecked(4) | (_buffer.GetUnchecked(5) << 8));
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
                ushort recvCrc = (ushort)(_buffer.GetUnchecked(totalPacketLen - 2) |
                                          (_buffer.GetUnchecked(totalPacketLen - 1) << 8));

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
                _buffer.CopyTo(6, payload, 0, dataLen);

                // Уведомляем потребителя
                _callback?.Invoke(payload, dataLen);

                // Удаляем весь пакет из буфера
                Consume(totalPacketLen);
            }
        }

        /// <summary>
        /// Ищет заголовок скользящим 32-битным окном: каждый байт буфера читается
        /// один раз, а не четыре (как при чтении по четыре байта на каждое смещение).
        /// </summary>
        private int FindHeader()
        {
            int avail = _buffer.Size;
            if (avail < 4) return -1;

            // Заголовок пишется как Little-Endian, поэтому в окне младший байт — самый ранний.
            uint window = (uint)(_buffer.GetUnchecked(0) |
                                 (_buffer.GetUnchecked(1) << 8) |
                                 (_buffer.GetUnchecked(2) << 16) |
                                 (_buffer.GetUnchecked(3) << 24));

            if (window == _expectedHeader)
                return 0;

            for (int offset = 1; offset <= avail - 4; offset++)
            {
                window = (window >> 8) | ((uint)_buffer.GetUnchecked(offset + 3) << 24);
                if (window == _expectedHeader)
                    return offset;
            }
            return -1;
        }

        private ushort ComputeCrc16FromBuffer(int len)
        {
            ushort crc = 0x0000;
            for (int i = 0; i < len; i++)
            {
                crc ^= (ushort)(_buffer.GetUnchecked(i) << 8);
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
        /// Удаляет count элементов с начала буфера за O(1).
        /// </summary>
        private void Consume(int count)
        {
            _buffer.Skip(count);
        }
    }
}
