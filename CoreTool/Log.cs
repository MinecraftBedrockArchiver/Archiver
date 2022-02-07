using System;

namespace CoreTool
{
    internal class Log
    {
        private string prefix;

        public Log() { }

        public Log(string prefix)
        {
            this.prefix = prefix;
        }

        public void Write(string message = "")
        {
            Console.WriteLine(GetPrefix() + message);
        }
        public void Write(object message)
        {
            Console.WriteLine(GetPrefix() + message.ToString());
        }

        public void WriteRaw(string message, params object[] arg)
        {
            string reset = "";
            if (message.StartsWith('\r'))
            {
                message = message.TrimStart('\r');
                reset = "\r";
            }

            Console.Write(reset + GetPrefix() + message, arg);
        }

        public void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetPrefix("ERROR") + message);
            Console.ResetColor();
        }

        public void WriteWarn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(GetPrefix("WARN") + message);
            Console.ResetColor();
        }

        private string GetPrefix(string type = "INFO")
        {
            return $"[{DateTime.Now.ToString("d")} {DateTime.Now.ToString("HH:mm:ss")} {type}]" + (prefix != null ? $"[{prefix}]" : "") + " ";
        }
    }
}
