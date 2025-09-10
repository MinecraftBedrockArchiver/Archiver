using CoreTool.Archive;
using StoreLib.Models;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CoreTool.Loaders.Windows
{
    internal class StoreLoader : ILoader
    {
        private string packageId;
        private string packageName;

        public StoreLoader(string packageId, string packageName)
        {
            this.packageId = packageId;
            this.packageName = packageName;
        }

        public async Task Load(ArchiveMeta archive)
        {
            // Create the dcat handler in production mode
            DisplayCatalogHandler dcathandler = DisplayCatalogHandler.ProductionConfig();

            // Create a packages var for debugging
            IList<PackageInstance> packages;
            string releaseVer = "";

            archive.Logger.Write("Loading store...");

            // Grab the packages for the release
            await dcathandler.QueryDCATAsync(this.packageId);
            if (dcathandler.Result == DisplayCatalogResult.Found)
            {
                packages = await dcathandler.GetPackagesForProductAsync();
                foreach (PackageInstance package in packages)
                {
                    if (!package.PackageMoniker.StartsWith(packageName + "_")) continue;
                    int platformTarget = package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget;
                    if (platformTarget != 0
                        && platformTarget != 3) continue;

                    string fullPackageName = package.PackageMoniker + (platformTarget == 0 ? ".Appx" : ".AppxBundle");

                    // Create the meta and store it
                    Item item = new Item(Utils.GetVersionFromName(fullPackageName));
                    item.Archs[Utils.GetArchFromName(fullPackageName)] = new Arch(fullPackageName, new List<string>() { Guid.Parse(package.UpdateId).ToString() });
                    if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(fullPackageName)}");

                    releaseVer = Utils.GetVersionFromName(fullPackageName);
                }
            }
        }
    }
}
