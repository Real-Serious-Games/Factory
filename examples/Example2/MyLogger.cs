using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logger
{
    public class MyLogger : ILogger
    {
        public void LogError(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void LogInfo(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message, params object[] args)
        {
            Console.WriteLine(message);
        }

        public void LogVerbose(string message, params object[] args)
        {
            Console.WriteLine(message);
        }
    }
}
