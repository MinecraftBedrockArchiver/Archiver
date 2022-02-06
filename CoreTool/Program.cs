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

            Log.Write("Getting token...");

            string token = Authentication.GetWUToken();

            // The user doesn't need to know this (useful for debugging)
            //Log.Write($"Got token: {token}");

            archiveMeta = new ArchiveMeta(archiveDir);

            // Pull updates
            archiveMeta.LoadFiles();
            await archiveMeta.LoadVersionDB();
            await archiveMeta.LoadLive(token);

            // Do checks and download missing files
            archiveMeta.CheckMeta();
            await archiveMeta.CheckFiles(token);

            Log.Write("Done startup!");
            Log.Write("Starting update checker");

            // Check for updates every 5 mins
            updateTimer = new Timer(5 * 60 * 1000);
            updateTimer.Elapsed += OnUpdateEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

            Log.Write("Press enter to exit at any point");
            Console.ReadLine();
        }

        private static async void OnUpdateEvent(object sender, ElapsedEventArgs e)
        {
            Log.Write("Checking for updates...");

            // Grab a new token incase the other expired
            string token = Authentication.GetWUToken();

            // Load the live data
            await archiveMeta.LoadLive(token);

            // Pull any missing files
            await archiveMeta.CheckFiles(token);
        }
    }
}
