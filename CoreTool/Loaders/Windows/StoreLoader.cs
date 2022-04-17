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
        private bool hasBeta;
        private bool authBetaQuery;
        private string scope;

        public StoreLoader(string packageId, string packageName, bool hasBeta = true, bool authBetaQuery = false, string scope = "service::dcat.update.microsoft.com::MBI_SSL")
        {
            this.packageId = packageId;
            this.packageName = packageName;
            this.hasBeta = hasBeta;
            this.authBetaQuery = authBetaQuery;
            this.scope = scope;
        }

        public async Task Load(ArchiveMeta archive)
        {
            // Create the dcat handler in production mode
            DisplayCatalogHandler dcathandler = DisplayCatalogHandler.ProductionConfig();

            // Create a packages var for debugging
            IList<PackageInstance> packages;
            string releaseVer = "";

            archive.Logger.Write("Loading release...");

            // Grab the packages for the release
            await dcathandler.QueryDCATAsync(this.packageId);
            if (dcathandler.Result == DisplayCatalogResult.Found)
            {
                packages = await dcathandler.GetPackagesForProductAsync();
                foreach (PackageInstance package in packages)
                {
                    if (!package.PackageMoniker.StartsWith(packageName + "_")) continue;
                    if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                    string fullPackageName = package.PackageMoniker + ".Appx";

                    // Create the meta and store it
                    Item item = new Item(Utils.GetVersionFromName(fullPackageName));
                    item.Archs[Utils.GetArchFromName(fullPackageName)] = new Arch(fullPackageName, new List<string>() { Guid.Parse(package.UpdateId).ToString() });
                    if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(fullPackageName)}");

                    releaseVer = Utils.GetVersionFromName(fullPackageName);
                }
            }

            if (!hasBeta) return;

            // Make sure we have a token, if not don't bother checking for betas
            string token = await Utils.GetMicrosoftToken("msAuthInfo.json", scope);
            if (token == "")
            {
                archive.Logger.WriteError("Failed to get token! Unable to fetch beta.");
            }
            else
            {
                archive.Logger.Write("Loading beta...");

                // Grab the packages for the beta using auth
                string authentication = "";
                if (authBetaQuery)
                {
                    authentication = "WLID1.0=" + Encoding.Unicode.GetString(Convert.FromBase64String(token));
                }
                await dcathandler.QueryDCATAsync(this.packageId, IdentiferType.ProductID, authentication);
                if (dcathandler.Result == DisplayCatalogResult.Found)
                {
                    packages = await dcathandler.GetPackagesForProductAsync($"<User>{await Utils.GetMicrosoftToken("msAuthInfo.json")}</User>");
                    foreach (PackageInstance package in packages)
                    {
                        if (!package.PackageMoniker.StartsWith(packageName + "_")) continue;
                        if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                        string fullPackageName = package.PackageMoniker + ".Appx";

                        // Check we haven't got a release version in the beta request
                        if (Utils.GetVersionFromName(fullPackageName) == releaseVer)
                        {
                            archive.Logger.WriteError($"You need to opt into the beta! Release version found in beta request. See https://aka.ms/JoinMCBeta");
                            break;
                        }

                        // Create the meta and store it
                        Item item = new Item(Utils.GetVersionFromName(fullPackageName));
                        item.Archs[Utils.GetArchFromName(fullPackageName)] = new Arch(fullPackageName, new List<string>() { Guid.Parse(package.UpdateId).ToString() });
                        if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(fullPackageName)}");
                    }
                }
            }
        }
    }
}
