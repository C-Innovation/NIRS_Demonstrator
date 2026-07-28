using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Читает уже принятые порт��м байты в <paramref name="buffer"/> и возвращает их число.
        /// Никогда не блокируется: если данных нет, сразу возвращает 0 — вызывающая сторона
        /// сама решает, ждать ей или нет.
        /// </summary>
        public async Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (_SerialPort == null || !_SerialPort.IsOpen)
                return 0;

            int available;
            try
            {
                available = _SerialPort.BytesToRead;
            }
            catch (InvalidOperationException)
            {
                // Порт закрыли между проверкой IsOpen и обращением к BytesToRead.
                return 0;
            }

            if (available <= 0)
                return 0;

            int toRead = Math.Min(available, buffer.Length);

            try
            {
                // Настоящий асинхронный ввод-вывод вместо Task.Run поверх блокирующего Read:
                // не занимает поток из пула на время обмена с портом.
                return await _SerialPort.BaseStream.ReadAsync(buffer, 0, toRead, cancellationToken)
                                                   .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return 0;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                // Порт закрыт/отключён во время чтения.
                return 0;
            }
        }

        /// <summary>
        /// Legacy-обёртка: выделяет массив под каждый вызов.
        /// Предпочтительна перегрузка с переиспользуемым буфером.
        /// </summary>
        public async Task<IEnumerable<byte>> ReadAsync()
        {
            if (_SerialPort == null || !_SerialPort.IsOpen)
                return null;

            byte[] buffer = new byte[Math.Max(_SerialPort.BytesToRead, 1)];
            int read = await ReadAsync(buffer).ConfigureAwait(false);
            if (read == buffer.Length)
                return buffer;

            byte[] exact = new byte[read];
            Array.Copy(buffer, exact, read);
            return exact;
        }

        public async Task<bool> WriteAsync(byte[] buf, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (buf == null)
                throw new ArgumentNullException(nameof(buf));

            if (_SerialPort == null || !_SerialPort.IsOpen)
                return false;

            try
            {
                await _SerialPort.BaseStream.WriteAsync(buf, offset, count, cancellationToken)
                                            .ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                return false;
            }
        }

        public Task<bool> WriteAsync(IEnumerable<byte> buf)
        {
            if (buf == null)
                return Task.FromResult(false);

            // Одна материализация вместо ToArray() + Count() (двойной обход перечислителя).
            byte[] bytes = buf as byte[] ?? buf.ToArray();
            return WriteAsync(bytes, 0, bytes.Length);
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
