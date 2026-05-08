using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace NIRS_Demonstrator
{
    public class UsbSerialPort : IDisposable
    {
        #region Private Members

        private readonly string _PortName;
        private readonly int _PortBaudRate;
        private SerialPort _SerialPort;
        private Mutex _SerialPortMutex;
        #endregion
        public UsbSerialPort(string portName, int baudRate)
        {
            
            _PortName = portName;
            _PortBaudRate = baudRate;
            _SerialPort = new SerialPort(_PortName, _PortBaudRate, Parity.None, 8, StopBits.One);
            _SerialPort.ReadBufferSize = (baudRate / 8) * 10;
            _SerialPortMutex = new Mutex(false);
        }

        public bool Start()
        {
            if (_SerialPort == null)
                return false;

            if (_SerialPort.IsOpen)
                return false;

            _SerialPort.Open();

            if (!_SerialPort.IsOpen)
                return false;

            return true;
        }

        public bool Stop()
        {
            if (_SerialPort == null)
                return false;

            if (!_SerialPort.IsOpen)
                return true;

            _SerialPort.Close();

            if (_SerialPort.IsOpen)
                return false;

            return true;
        }

        public async Task<IEnumerable<byte>> ReadAsync()
        {
            if (_SerialPort == null)
                return null;

            if (!_SerialPort.IsOpen)
                return null;
            
            byte[] _tBytes = new byte[_SerialPort.BytesToRead];
            //await _SerialPort.BaseStream.ReadAsync(_tBytes, 0, _tBytes.Length);
            await Task.Run(() =>
            {
                _SerialPort.Read(_tBytes, 0, _tBytes.Length);
            });
            return _tBytes;
        }

        public async Task<bool> WriteAsync(IEnumerable<byte> buf)
        {
            if (_SerialPort == null)
                return false;

            if (!_SerialPort.IsOpen)
                return false;

            await _SerialPort.BaseStream.WriteAsync(buf.ToArray(), 0, buf.Count());
            return true;
        }

        public void Dispose()
        {
            if (_SerialPort != null)
            {
                if(_SerialPort.IsOpen)
                {
                    _SerialPort.Close();
                    _SerialPort.Dispose();
                }
            }
        }

        public static IEnumerable<string> GetPortNames()
        {
            return SerialPort.GetPortNames();
        }

        
    }
}
