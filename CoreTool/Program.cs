using CoreTool.Checkers;
using CoreTool.Loaders;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace CoreTool
{
    class Program
    {
        private static Timer updateTimer;
        private static ArchiveMeta archiveMetaW10;
        private static ArchiveMeta archiveMetaXbox;

        static async Task Main(string[] args)
        {
            // Set the archive dir
            // TODO: Make this configurable
            string archiveDirW10 = @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\";
            string archiveDirXbox = @"\\192.168.1.5\Archive\Minecraft\Xbox - Microsoft.MinecraftUWPConsole_8wekyb3d8bbwe\";

            archiveMetaW10 = new ArchiveMeta("W10", archiveDirW10, new List<ILoader>()
            {
                new FileLoader(),
                new VersionDBLoader(),
                new StoreLoader("9NBLGGH2JHXJ", "Microsoft.MinecraftUWP")
            }, new List<IChecker>()
            {
                new MetaChecker(),
                new FileChecker(),
            });

            archiveMetaXbox = new ArchiveMeta("Xbox", archiveDirXbox, new List<ILoader>()
            {
                new FileLoader(),
                new StoreLoader("9NBLGGH537BL", "Microsoft.MinecraftUWPConsole")
            }, new List<IChecker>()
            {
                new MetaChecker(),
                new FileChecker(),
            });

            // Load data
            await archiveMetaW10.Load();
            await archiveMetaXbox.Load();

            // Do checks and download missing files
            await archiveMetaW10.Check();
            await archiveMetaXbox.Check();

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

            // Windows 10
            await archiveMetaW10.Load();
            await archiveMetaW10.Check();

            // Xbox
            await archiveMetaXbox.Load();
            await archiveMetaXbox.Check();
        }
    }
}
