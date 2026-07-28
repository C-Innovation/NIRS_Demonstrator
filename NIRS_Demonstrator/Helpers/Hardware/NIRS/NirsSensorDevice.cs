using DynamicData;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class NirsSensorDevice : IDisposable
    {
        #region Protected Members

        #endregion

        #region Private Members
        const uint HEADER = 0x234E5253;
        private const int NIRS_UART_BAUDRATE = 4000000;
        private const int NIRS_QUEUE_SIZE = 100_000;
        private const int READ_CHUNK_SIZE = 64 * 1024;
        private readonly UsbSerialPort _SerialPort;
        private Thread _NirsDataThread;
        private volatile bool _NirsDataThreadStarted;
        private readonly Queue<NirsSensorData> _AvailebleDataQueue;
        private readonly Queue<NirsSensorFilteredData> _AvailebleFilteredDataQueue;
        private readonly CircularBuffer<byte> _RawBuffer;
        private readonly Deserializer _Deserializer;
        private readonly Serializer _Serializer;
        private readonly byte[] _ReadBuffer = new byte[READ_CHUNK_SIZE];

        /// <summary>
        /// Сигнализирует потребителям о появлении данных, чтобы они не опрашивали
        /// очередь по таймеру.
        /// </summary>
        private readonly SemaphoreSlim _DataAvailableSignal = new SemaphoreSlim(0, 1);

        private bool _IsStarted = false;
        private bool _IsDisposed = false;
        #endregion

        #region Public Properties

        public bool IsStarted => _IsStarted;
        public double TimeStart { get; set; } = 0;
        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public NirsSensorDevice(string interfaceName)
        {
            _SerialPort = new UsbSerialPort(interfaceName, NIRS_UART_BAUDRATE);
            _AvailebleDataQueue = new Queue<NirsSensorData>(NIRS_QUEUE_SIZE);
            _AvailebleFilteredDataQueue = new Queue<NirsSensorFilteredData>(NIRS_QUEUE_SIZE);
            _RawBuffer = new CircularBuffer<byte>(NIRS_QUEUE_SIZE * 64);
            _Deserializer = new Deserializer(_RawBuffer, HEADER, DataDeselializeComplete);
            _Serializer = new Serializer(HEADER);
            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        #endregion

        #region Private Callbacks

        private void DataDeselializeComplete(byte[] data, int len)
        {
            if (data[0] == 0x01) {
                lock (_AvailebleDataQueue)
                {
                    byte[] b = new byte[len - 1];
                    Array.Copy(data, 1, b, 0, len - 1);
                    _AvailebleDataQueue.Enqueue(b.ToNirsSensorData());

                }
                SignalDataAvailable();
            }

            if (data[0] == 0x02)
            {
                lock (_AvailebleFilteredDataQueue)
                {
                    byte[] b = new byte[len - 1];
                    Array.Copy(data, 1, b, 0, len - 1);
                    _AvailebleFilteredDataQueue.Enqueue(b.ToNirsSensorFilteredData());
                }
                SignalDataAvailable();
            }

        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _AvailebleDataQueue.Clear();
            _AvailebleFilteredDataQueue.Clear();
            _RawBuffer.Clear();

            _SerialPort.Start();
            _NirsDataThreadStarted = true;
            _IsStarted = true;
            _NirsDataThread = new Thread(NirsDataThreadAction);
            _NirsDataThread.Start();
        }

        public void Stop()
        {

            if (_NirsDataThreadStarted)
            {
                _NirsDataThreadStarted = false;
                _NirsDataThread.Join();
            }
            _SerialPort.Stop();
            _IsStarted = false;

            // Разбудить потребителей, ожидающих данные, чтобы они увидели остановку.
            SignalDataAvailable();
        }

        /// <summary>
        /// Ожидает появления новых данных либо истечения таймаута.
        /// Заменяет опрос очереди фиксированным Task.Delay: потребитель просыпается
        /// по факту прихода пакета, а не по таймеру.
        /// </summary>
        public Task<bool> WaitForDataAsync(int timeoutMs, CancellationToken cancellationToken = default)
        {
            return _DataAvailableSignal.WaitAsync(timeoutMs, cancellationToken);
        }

        public List<NirsSensorData> GetAvailebleData()
        {
            List<NirsSensorData> data = new List<NirsSensorData>();
            lock (_AvailebleDataQueue)
            {
                while (_AvailebleDataQueue.Count > 0)
                    data.Add(_AvailebleDataQueue.Dequeue());
            }
            return data;
        }

        public List<NirsSensorFilteredData> GetAvailebleFilteredData()
        {
            List<NirsSensorFilteredData> data = new List<NirsSensorFilteredData>();
            lock (_AvailebleFilteredDataQueue)
            {
                while (_AvailebleFilteredDataQueue.Count > 0)
                    data.Add(_AvailebleFilteredDataQueue.Dequeue());
            }
            return data;
        }

        public async Task SetIrLedCurrentProcAsync(float proc)
        {
            List<byte> bytes = new List<byte>();
            bytes.Add(0x04);
            bytes.Add(0x02);
            bytes.AddRange(BitConverter.GetBytes(proc));
            var outbytes = _Serializer.Serialize(bytes.ToArray());
            await _SerialPort.WriteAsync(outbytes.ToArray());
        }

        public void Dispose()
        {
            if (_IsDisposed)
                return;

            _IsDisposed = true;
            Stop();
            _SerialPort.Dispose();
            _DataAvailableSignal.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Освобождает одно ожидание, если оно ещё не выдано.
        /// Счётчик семафора намеренно не растёт: потребитель всё равно вычитывает
        /// очередь целиком за одно пробуждение.
        /// </summary>
        private void SignalDataAvailable()
        {
            if (_DataAvailableSignal.CurrentCount == 0)
            {
                try
                {
                    _DataAvailableSignal.Release();
                }
                catch (SemaphoreFullException)
                {
                    // Гонка с другим потоком-производителем — сигнал уже выставлен.
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private async void NirsDataThreadAction()
        {
            while(_NirsDataThreadStarted)
            {
                // Читаем в переиспользуемый буфер и переносим всё разом:
                // одно Array.Copy вместо PushBack на каждый байт.
                int read = await _SerialPort.ReadAsync(_ReadBuffer).ConfigureAwait(false);

                if (read > 0)
                {
                    _RawBuffer.PushBack(_ReadBuffer, 0, read);
                    _Deserializer.Process();

                    // Данные шли сплошным потоком — сразу пробуем дочитать остаток,
                    // не тратя 5 мс на паузу.
                    if (read == _ReadBuffer.Length)
                        continue;
                }

                await Task.Delay(5).ConfigureAwait(false);
            }

        }

        #endregion
    }
}
