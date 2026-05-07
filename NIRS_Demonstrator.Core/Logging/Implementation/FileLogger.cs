using System;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class FileLogger : ILogger
    {
        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool LogTime { get; set; } = true;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public FileLogger(string filePath)
        {
            //
            FilePath = filePath;
        }

        #endregion

        #region Logger Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public void Log(string message, LogLevel level)
        {
            //
            var currentTime = DateTimeOffset.Now.ToString("yyyy-MM-dd  hh:mm:ss");

            //
            var timeLogString = LogTime ? $"[{currentTime}] " : "";
            
            //
            IoC.File.WriteTextToFileAsync($"{timeLogString}{message}{Environment.NewLine}", FilePath, append: true);
        } 

        #endregion
    }
}
