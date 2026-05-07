namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogger
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        void Log(string message, LogLevel level);

    }
}
