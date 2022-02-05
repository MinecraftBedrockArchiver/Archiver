using System;
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
            String token;
            if (GetWUToken(out token) != 0)
            {
                token = "";
            }
            return token;
        }
    }
}
