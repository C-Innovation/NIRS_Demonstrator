using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace NIRS_Demonstrator.Core
{

    /// <summary>
    /// 
    /// 
    /// </summary>
    public class BaseLogFactory : ILogFactory
    {

        #region Protected Methods

        /// <summary>
        /// 
        /// </summary>
        protected List<ILogger> mLoggers = new List<ILogger>();

        /// <summary>
        /// 
        /// </summary>
        protected object mLoggersLock = new object();

        #endregion

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public LogOutputLevel LogOutputLevel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IncludeLogOriginDetails { get; set; } = true;

        #endregion

        #region Public Events

        /// <summary>
        /// 
        /// </summary>
        public event Action<(string Message, LogLevel Level)> NewLog = (details) => { };

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggers"></param>
        public BaseLogFactory(ILogger[] loggers = null)
        {
            //
            AddLogger(new DebugLogger());

            //
            if (loggers != null)
                foreach (var logger in loggers)
                    AddLogger(logger);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public void AddLogger(ILogger logger)
        {
            //
            lock (mLoggersLock)
            {
                //
                if (!mLoggers.Contains(logger))
                    //
                    mLoggers.Add(logger);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public void RemoveLogger(ILogger logger)
        {
            //
            lock (mLoggersLock)
            {
                //
                if (mLoggers.Contains(logger))
                    //
                    mLoggers.Remove(logger);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="origin"></param>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        public void Log(string message, LogLevel level = LogLevel.Informative,[CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            //
            if ((int)level < (int)LogOutputLevel)
                return;

            //
            if (IncludeLogOriginDetails)
                message = $"{message} [{Path.GetFileName(filePath)} > {origin} > Line {lineNumber}]";

            // Log the list so it is thread-safe
            lock (mLoggersLock)
            {
                // Log to all loggers
                mLoggers.ForEach(logger => logger.Log(message, level));
            }

            // Inform listeners
            NewLog.Invoke((message,level));
        }

        #endregion
    }
}
