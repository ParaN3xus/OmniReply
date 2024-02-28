using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniReply.CommonUtils
{
    public static class Log
    {
        public enum LogLevel { Debug = 0, Info = 1, Warning = 2, Error = 3 }

        public static void WriteLog(string message, LogLevel level)
        {
            if (level == LogLevel.Debug)
            {
#if !DEBUG
                return;
#endif
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else if (level == LogLevel.Info)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (level == LogLevel.Warning)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
            }
            else if (level == LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        }
    }
}
