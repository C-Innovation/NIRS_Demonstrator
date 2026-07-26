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
        private readonly UsbSerialPort _SerialPort;
        private Thread _NirsDataThread;
        private bool _NirsDataThreadStarted;
        private readonly Queue<NirsSensorData> _AvailebleDataQueue;
        private readonly Queue<NirsSensorFilteredData> _AvailebleFilteredDataQueue;
        private readonly CircularBuffer<byte> _RawBuffer;
        private readonly Deserializer _Deserializer;
        private readonly Serializer _Serializer;
        private bool _IsStarted = false;
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

        ~NirsSensorDevice()
        {
            Dispose();
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
            }

            if (data[0] == 0x02)
            {
                lock (_AvailebleFilteredDataQueue)
                {
                    byte[] b = new byte[len - 1];
                    Array.Copy(data, 1, b, 0, len - 1);
                    _AvailebleFilteredDataQueue.Enqueue(b.ToNirsSensorFilteredData());
                }
            }
            
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _AvailebleDataQueue.Clear();
            _AvailebleFilteredDataQueue.Clear();
            while(_RawBuffer.Size > 0)
                _RawBuffer.PopFront();

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
            Stop();
        }

        #endregion

        #region Private Methods

        private async void NirsDataThreadAction()
        {
            while(_NirsDataThreadStarted)
            {
                byte[] _dataRaw = (byte[])(await _SerialPort.ReadAsync());
                for (int i = 0; i <_dataRaw.Length; i++)
                {
                    _RawBuffer.PushBack(_dataRaw[i]);
                }
                _Deserializer.Process();
                await Task.Delay(5);
            }

        }

        #endregion
    }
}
