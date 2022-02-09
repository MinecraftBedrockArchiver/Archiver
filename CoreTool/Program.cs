using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using CoreTool.Archive;

namespace CoreTool
{
    class Program
    {
        private static Timer updateTimer;
        private static List<ArchiveMeta> archives;

        static async Task Main(string[] args)
        {
            // Load the archives from the config.json
            archives = Config.Loader.Load();

            // Load data
            foreach (ArchiveMeta archive in archives)
            {
                await archive.Load();
            }

            // Do checks and download missing files
            foreach (ArchiveMeta archive in archives)
            {
                await archive.Check();
            }

            Utils.GenericLogger.Write("Done startup!");
            Utils.GenericLogger.Write("Starting update checker");

            // Check for updates every 5 mins
            updateTimer = new Timer(5 * 60 * 1000);
            updateTimer.Elapsed += OnUpdateEvent;
            updateTimer.AutoReset = true;
            updateTimer.Enabled = true;

            Utils.GenericLogger.Write("Press enter to exit at any point");
            Console.ReadLine();
        }

        private static async void OnUpdateEvent(object sender, ElapsedEventArgs e)
        {
            Utils.GenericLogger.Write("Checking for updates...");

            // Grab a new token incase the other expired
            string token = Authentication.GetWUToken();

            foreach (ArchiveMeta archive in archives)
            {
                await archive.Load();
                await archive.Check();
            }
        }
    }
}
