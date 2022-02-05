using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using StoreLib.Models;
using StoreLib.Services;

namespace MinecraftW10Downloader
{
    class Program
    {

        static async Task Main(string[] args)
        {
            DisplayCatalogHandler dcathandler = DisplayCatalogHandler.ProductionConfig();
            IList<PackageInstance> packages;

            Console.WriteLine("Getting token...");


            string token = await Authentication.GetWUToken();
            if (token == "")
            {
                Console.WriteLine("Failed to get token! Unable to fetch beta.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Fetching Beta... (if this returns release versions then you need to opt into the beta)");

                await dcathandler.QueryDCATAsync("9nblggh2jhxj", IdentiferType.ProductID, "Bearer WLID1.0=" + Convert.FromBase64String(token));
                packages = await dcathandler.GetPackagesForProductAsync($"<User>{token}</User>");
                foreach (PackageInstance package in packages)
                {
                    if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                    if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                    Console.WriteLine("{0}\t{1}\t{2}", package.PackageMoniker, package.UpdateId, package.PackageUri);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Fetching Release...");

            await dcathandler.QueryDCATAsync("9nblggh2jhxj");
            packages = await dcathandler.GetPackagesForProductAsync();
            foreach (PackageInstance package in packages)
            {
                if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                Console.WriteLine("{0}\t{1}\t{2}", package.PackageMoniker, package.UpdateId, package.PackageUri);
            }

            Console.ReadLine();
        }
    }
}
