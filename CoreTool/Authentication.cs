using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MinecraftW10Downloader
{
    class Authentication
    {
        [DllImport("WUTokenHelper.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern int GetWUToken([MarshalAs(UnmanagedType.LPWStr)] out string token);

        public static async Task<String> GetWUToken()
        {
            // Get the local token file
            if (File.Exists("token.txt"))
            {
                Console.WriteLine("Read token from file");
                return File.ReadAllText("token.txt");
            }

            // Get the token using the token helper
            String token;
            if (GetWUToken(out token) != 0)
            {
                token = "";
            }
            return token;
        }
    }
}
