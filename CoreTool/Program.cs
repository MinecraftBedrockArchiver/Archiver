using System;
using System.Threading.Tasks;
using System.Timers;

namespace CoreTool
{
    class Program
    {
        private static Timer updateTimer;
        private static ArchiveMeta archiveMeta;

        static async Task Main(string[] args)
        {
            // Set the archive dir
            // TODO: Make this configurable
            string archiveDir = @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\";

            Console.WriteLine("Getting token...");

            string token = Authentication.GetWUToken();

            // The user doesn't need to know this (useful for debugging)
            //Console.WriteLine($"Got token: {token}");

            archiveMeta = new ArchiveMeta(archiveDir);

            // Pull updates
            archiveMeta.LoadFiles();
            await archiveMeta.LoadVersionDB();
            await archiveMeta.LoadLive(token);

            // Do checks and download missing files
            archiveMeta.CheckMeta();
            await archiveMeta.CheckFiles(token);

            Console.WriteLine("Done startup!");
            Console.WriteLine("Starting update checker");

            // Check for updates every 5 mins
            updateTimer = new Timer(5 * 60 * 1000);
            updateTimer.Elapsed += OnUpdateEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

            Console.ReadLine();
        }

        private static async void OnUpdateEvent(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Checking for updates...");

            // Grab a new token incase the other expired
            string token = Authentication.GetWUToken();

            // Load the live data
            await archiveMeta.LoadLive(token);

            // Pull any missing files
            await archiveMeta.CheckFiles(token);
        }
    }
}
