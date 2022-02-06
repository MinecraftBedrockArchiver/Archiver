using System;
using System.Threading.Tasks;

namespace CoreTool
{
    class Program
    {

        static async Task Main(string[] args)
        {
            // Set the archive dir
            // TODO: Make this configurable
            string archiveDir = @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\";

            Console.WriteLine("Getting token...");

            string token = Authentication.GetWUToken();

            // The user doesn't need to know this (useful for debugging)
            //Console.WriteLine($"Got token: {token}");

            ArchiveMeta archiveMeta = new ArchiveMeta(archiveDir);

            // Pull updates
            archiveMeta.LoadFiles();
            await archiveMeta.LoadVersionDB();
            await archiveMeta.LoadLive(token);

            // Do checks and download missing files
            archiveMeta.CheckMeta();
            await archiveMeta.CheckFiles(token);

            Console.ReadLine();
        }
    }
}
