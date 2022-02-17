using System;
using System.Threading.Tasks;
using System.Timers;
using CoreTool.Archive;

namespace CoreTool
{
    class Program
    {
        private static Timer updateTimer;
        private static GitSync gitSync;

        static async Task Main(string[] args)
        {
            if (Config.Loader.Config.GitSync.Enabled)
            {
                gitSync = new GitSync();
                await gitSync.Load();
            }

            // Load data
            foreach (ArchiveMeta archive in Config.Loader.Config.ArchiveInstances)
            {
                await archive.Load();
            }

            // Do checks and download missing files
            foreach (ArchiveMeta archive in Config.Loader.Config.ArchiveInstances)
            {
                await archive.Check();
            }

            // Run GitSync if enabled
            if (Config.Loader.Config.GitSync.Enabled)
            {
                await gitSync.Check();
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
            // Stop the timer before we check
            // This stops the timer ticking while we are still checking
            updateTimer.Enabled = false;

            Utils.GenericLogger.Write("Checking for updates...");

            foreach (ArchiveMeta archive in Config.Loader.Config.ArchiveInstances)
            {
                await archive.Load();
                await archive.Check();
            }

            // Run GitSync if enabled
            if (Config.Loader.Config.GitSync.Enabled)
            {
                await gitSync.Check();
            }

            // Resume the timer
            updateTimer.Enabled = true;
        }
    }
}
