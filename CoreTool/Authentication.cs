using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MinecraftW10Downloader
{
    class Authentication
    {
        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetWUToken([MarshalAs(UnmanagedType.LPWStr)] out string token);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        static public extern IntPtr GetStdHandle(uint nStdHandle);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        static public extern bool SetStdHandle(uint nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        static public extern IntPtr CreateConsoleScreenBuffer(uint dwDesiredAcess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwFlags, IntPtr lpScreenBufferData);

        const uint accessMode = 0x40000000; // GENERIC_WRITE
        const uint STD_OUTPUT_HANDLE = unchecked((uint)-11);
        const uint STD_ERROR_HANDLE = unchecked((uint)-12);

        public static string GetWUToken()
        {
            // Get the local token file
            if (File.Exists("token.txt"))
            {
                return File.ReadAllText("token.txt");
            }

            String token;

            // Redirect console output to nothing so we hide the dll output
            // From: https://social.msdn.microsoft.com/Forums/en-US/c89e2f26-9fb2-46f3-a138-591cb91c5105/#5a6fc798-4ed4-464d-b6bd-81c22fb9771f-isAnswer
            IntPtr hBuffer = CreateConsoleScreenBuffer(accessMode, 0, IntPtr.Zero, 1, IntPtr.Zero);
            IntPtr hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            IntPtr hStdErr = GetStdHandle(STD_ERROR_HANDLE);
            if (SetStdHandle(STD_OUTPUT_HANDLE, hBuffer) && SetStdHandle(STD_ERROR_HANDLE, hBuffer))
            {
                // Get the token using the token helper
                if (GetWUToken(out token) != 0)
                {
                    token = "";
                }

                // Restore original console buffers before calling Console.WriteLine
                if (!SetStdHandle(STD_OUTPUT_HANDLE, hStdOut) || !SetStdHandle(STD_ERROR_HANDLE, hStdErr))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return token;
        }
    }
}
