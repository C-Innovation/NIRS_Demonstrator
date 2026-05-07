using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private const int NIRS_UART_BAUDRATE = 1_000_000;
        private const int NIRS_QUEUE_SIZE = 1_000;
        private readonly UsbSerialPort _SerialPort;
        private Thread _NirsDataThread;
        private bool _NirsDataThreadStarted;
        private readonly Queue<NirsSensorData> _AvailebleDataQueue;
        private readonly CircularBuffer<byte> _RawBuffer;
        private readonly Deserializer _Deserializer;
        #endregion

        #region Public Properties

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
            _RawBuffer = new CircularBuffer<byte>(NIRS_QUEUE_SIZE * 64);
            _Deserializer = new Deserializer(_RawBuffer, HEADER, DataDeselializeComplete);
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
            _AvailebleDataQueue.Enqueue(data.ToNirsSensorData());
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            _AvailebleDataQueue.Clear();
            while(_RawBuffer.Size > 0)
                _RawBuffer.PopFront();

            _SerialPort.Start();
            _NirsDataThreadStarted = true;
            _NirsDataThread = new Thread(NirsDataThreadAction);
            _NirsDataThread.Start();
        }

        public void Stop()
        {
            _SerialPort.Stop();
            if (_NirsDataThreadStarted)
            {
                _NirsDataThreadStarted = false;
                _NirsDataThread.Join();
            }
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
