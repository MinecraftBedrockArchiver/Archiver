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
        private static List<ArchiveMeta> archiveMetas = new List<ArchiveMeta>();

        static async Task Main(string[] args)
        {
            // TODO: Make these configurable

            archiveMetas.Add(new ArchiveMeta("W10", @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\", new List<ILoader>()
            {
                new FileLoader(),
                new VersionDBLoader(),
                new StoreLoader("9NBLGGH2JHXJ", "Microsoft.MinecraftUWP")
            }, new List<IChecker>()
            {
                new MetaChecker(),
                new FileChecker(),
            }));

            archiveMetas.Add(new ArchiveMeta("Xbox", @"\\192.168.1.5\Archive\Minecraft\Xbox - Microsoft.MinecraftUWPConsole_8wekyb3d8bbwe\", new List<ILoader>()
            {
                new FileLoader(),
                new StoreLoader("9NBLGGH537BL", "Microsoft.MinecraftUWPConsole")
            }, new List<IChecker>()
            {
                new MetaChecker(),
                new FileChecker(),
            }));

            archiveMetas.Add(new ArchiveMeta("Preview", @"\\192.168.1.5\Archive\Minecraft\Microsoft.MinecraftUWPBeta_8wekyb3d8bbwe\", new List<ILoader>()
            {
                new FileLoader(),
                new StoreLoader("9MTK992XRFL2", "Microsoft.MinecraftUWPBeta", true, "service::www.microsoft.com::mbi_ssl")
            }, new List<IChecker>()
            {
                new MetaChecker(),
                new FileChecker(),
            }));

            // Load data
            foreach (ArchiveMeta meta in archiveMetas)
            {
                await meta.Load();
            }

            // Do checks and download missing files
            foreach (ArchiveMeta meta in archiveMetas)
            {
                await meta.Check();
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

            foreach (ArchiveMeta meta in archiveMetas)
            {
                await meta.Load();
                await meta.Check();
            }
        }
    }
}
