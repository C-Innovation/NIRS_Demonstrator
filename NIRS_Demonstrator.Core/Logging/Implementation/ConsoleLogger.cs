using System;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public void Log(string message, LogLevel level)
        {
            //
            var consoleOldColor = Console.ForegroundColor;

            //                      $"[{level}]".PadRight(13,' ') +
            var consoleColor = ConsoleColor.White;

            //
            switch(level)
            {

                //
                case LogLevel.Debug:
                    consoleColor = ConsoleColor.Blue;
                    break;


                //
                case LogLevel.Verbose:
                    consoleColor = ConsoleColor.Gray;
                    break;

                //
                case LogLevel.Warning:
                    consoleColor = ConsoleColor.DarkYellow;
                    break;

                //
                case LogLevel.Error:
                    consoleColor = ConsoleColor.Red;
                    break;

                //
                case LogLevel.Success:
                    consoleColor = ConsoleColor.Green;
                    break;
            }

            //
            Console.ForegroundColor = consoleColor;

            //
            Console.WriteLine(message);

            //
            Console.ForegroundColor = consoleOldColor;
        }
    }
}
