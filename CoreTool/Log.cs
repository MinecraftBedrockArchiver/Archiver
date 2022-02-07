using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class Log
    {
        public static void Write(string message = "")
        {
            Console.WriteLine(GetPrefix() + message);
        }
        public static void Write(object message)
        {
            Console.WriteLine(GetPrefix() + message.ToString());
        }

        public static void WriteRaw(string message, params object[] arg)
        {
            Console.Write(GetPrefix() + message, arg);
        }

        public static void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetPrefix("ERROR") + message);
            Console.ResetColor();
        }

        public static void WriteWarn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(GetPrefix("WARN") + message);
            Console.ResetColor();
        }

        private static string GetPrefix(string type = "INFO")
        {
            return $"[{DateTime.Now.ToString("d")} {DateTime.Now.ToString("HH:mm:ss")} {type}] ";
        }
    }
}
