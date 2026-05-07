using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NIRS_Demonstrator
{
    public static class DataHelpers
    {
        public static string MacToMaskedMac(string mac)
        {
            if (mac.Length < 12)
                return "[ N/A ]";
            string macString = "[";
            for (int i = 0; i < 5; i++)
            {
                macString += mac.Substring(2 * i, 2);
                macString += ":";
            }
            macString += mac.Substring(10, 2) + "]";

            return macString;
        }

        public static string GetCurrentDateTimeStr()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        private static readonly DateTimeOffset UnixEpoch =
        new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static long ToUnixTimeMicroseconds()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan duration = now - UnixEpoch;
            // There are 10 ticks per microsecond.
            return duration.Ticks / 10;
        }
    }
}
