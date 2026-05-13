using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    public class ReportsStreamerCsv : IDisposable
    {
        #region Protected members

        protected StreamWriter _ReportWriter;

        #endregion

        #region Private members

        private bool _writeInProcess = false;
        private Queue<List<double>> _writingQueue;

        private Thread _writeThread;
        private bool _writeThreadStarted = false;
        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="path">Full path to report file</param>
        public ReportsStreamerCsv(string path)
        {
            _ReportWriter = new StreamWriter(path);
            _writingQueue = new Queue<List<double>>();
            //AppConfig.GetInstance().RegisterDisposableObject(this);
            _writeThreadStarted = true;
            _writeThread = new Thread(WriteThreadActionAsync);
            _writeThread.Start();
        }


        public ReportsStreamerCsv()
        {
            string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr() + ".csv"));
            _ReportWriter = new StreamWriter(path);
            _writingQueue = new Queue<List<double>>();
            //AppConfig.GetInstance().RegisterDisposableObject(this);
            _writeThreadStarted = true;
            _writeThread = new Thread(WriteThreadActionAsync);
            _writeThread.Start();
        }

        ~ReportsStreamerCsv()
        {
            Dispose();
        }
        #endregion

        #region PublicMethods

        public async Task WriteAsync(char[]? buffer)
        {
            _writeInProcess = true;
            await _ReportWriter.WriteAsync(buffer);
            _writeInProcess = false;
        }

        public async Task WriteHeaderAsync(List<string> csvDataHeadersLine)
        {
            string csvDataLineStr = string.Empty;
            foreach (string header in csvDataHeadersLine)
            {
                csvDataLineStr += $"{header};";
            }
            csvDataLineStr += Environment.NewLine;
            await _ReportWriter.WriteAsync(csvDataLineStr);
        }

        public void Write(List<double> csvDataLine)
        {
            _writingQueue.Enqueue(csvDataLine);
        }

        public async void Dispose()
        {
            if (_ReportWriter != null)
            {
                while (_writeInProcess)
                    await Task.Delay(1);
                _ReportWriter.Close();
                _ReportWriter.Dispose();
                _writingQueue.Clear();

                if (_writeThreadStarted)
                {
                    _writeThreadStarted = false;
                    _writeThread.Join();
                }
                
            }

            GC.SuppressFinalize(this);
        }



        #endregion

        #region Private Methods

        private async void WriteThreadActionAsync()
        {
            while(_writeThreadStarted)
            {
                _writeInProcess = true;
                while (_writingQueue.Count > 0 )
                {
                    List<double> csvDataLine = _writingQueue.Dequeue();
                    string csvDataLineStr = string.Empty;
                    foreach (double data in csvDataLine)
                    {
                        csvDataLineStr += $"{data:0.000};";
                    }
                    csvDataLineStr += Environment.NewLine;
                    await _ReportWriter.WriteAsync(csvDataLineStr);

                }
                _writeInProcess = false;
                await Task.Delay(1);
            }
        }

        #endregion
    }
}
