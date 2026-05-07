using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    public class ReportsStreamer : IDisposable
    {
        #region Protected members

        protected StreamWriter _ReportWriter;

        #endregion

        #region Private members

        private bool _writeInProcess = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="path">Full path to report file</param>
        public ReportsStreamer(string path)
        {
            _ReportWriter = new StreamWriter(path);
        }


        public ReportsStreamer()
        {
            string path = Path.Combine(AppConfig.GetInstance().ReportsDirectoryPath, (DataHelpers.GetCurrentDateTimeStr() + ".txt"));
            _ReportWriter = new StreamWriter(path);
        }
        #endregion

        #region PublicMethods

        public async Task WriteAsync(char[]? buffer)
        {
            _writeInProcess |= true;
            await _ReportWriter.WriteAsync(buffer);
            _writeInProcess = false;
        }

        public async Task WriteAsync(string str)
        {
            while (_writeInProcess)
            {
                await Task.Delay(1);
            }
            _writeInProcess = true;
            long unixTime = DataHelpers.ToUnixTimeMicroseconds(); ;
            await _ReportWriter.WriteAsync($"[{unixTime}]\t{str}");
            _writeInProcess = false;
        }

        public async void Dispose()
        {
            if (_ReportWriter != null)
            {
                while (_writeInProcess)
                    await Task.Delay(1);
                _ReportWriter.Close();
                _ReportWriter.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
