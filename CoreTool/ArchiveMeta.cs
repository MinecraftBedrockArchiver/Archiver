using Newtonsoft.Json;
using StoreLib.Models;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class ArchiveMeta
    {
        private string archiveDir;
        private string archiveMetaFile;
        private Dictionary<string, MetaItem> metaItems;

        public ArchiveMeta(string archiveDir)
        {
            this.archiveDir = archiveDir;
            this.archiveMetaFile = Path.Join(archiveDir, "archive_meta.json");

            // Load the meta or create a new one
            if (File.Exists(archiveMetaFile))
            {
                metaItems = JsonConvert.DeserializeObject<Dictionary<string, MetaItem>>(File.ReadAllText(archiveMetaFile));
            }
            else
            {
                metaItems = new Dictionary<string, MetaItem>();
                Save();
            }
        }

        #region Data loaders
        internal async Task LoadLive(string token)
        {
            // Create the dcat handler in production mode
            DisplayCatalogHandler dcathandler = DisplayCatalogHandler.ProductionConfig();

            // Create a packages var for debugging
            IList<PackageInstance> packages;
            string releaseVer = "";
            
            Log.Write("Loading release...");

            // Grab the packages for the release
            await dcathandler.QueryDCATAsync("9NBLGGH2JHXJ");
            packages = await dcathandler.GetPackagesForProductAsync();
            foreach (PackageInstance package in packages)
            {
                if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                // Create the meta and store it
                MetaItem item = new MetaItem(Utils.GetVersionFromName(package.PackageMoniker));
                item.Archs[Utils.GetArchFromName(package.PackageMoniker)] = new MetaItemArch(package.PackageMoniker, new List<Guid>() { Guid.Parse(package.UpdateId) });
                if (AddOrUpdate(item, true)) Log.Write($"New version registered: {Utils.GetVersionFromName(package.PackageMoniker)}");

                releaseVer = Utils.GetVersionFromName(package.PackageMoniker);
            }

            // Make sure we have a token, if not don't bother checking for betas
            if (token == "")
            {
                Log.WriteError("Failed to get token! Unable to fetch beta.");
            }
            else
            {
                Log.Write("Loading beta...");

                // Grab the packages for the beta using auth
                await dcathandler.QueryDCATAsync("9NBLGGH2JHXJ", IdentiferType.ProductID, "Bearer WLID1.0=" + Convert.FromBase64String(token));
                packages = await dcathandler.GetPackagesForProductAsync($"<User>{token}</User>");
                foreach (PackageInstance package in packages)
                {
                    if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                    if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                    // Check we haven't got a release version in the beta request
                    if (Utils.GetVersionFromName(package.PackageMoniker) == releaseVer)
                    {
                        Log.WriteError($"You need to opt into the beta! Release version found in beta request. See https://aka.ms/JoinMCBeta");
                        break;
                    }

                    // Create the meta and store it
                    MetaItem item = new MetaItem(Utils.GetVersionFromName(package.PackageMoniker));
                    item.Archs[Utils.GetArchFromName(package.PackageMoniker)] = new MetaItemArch(package.PackageMoniker, new List<Guid>() { Guid.Parse(package.UpdateId) });
                    if (AddOrUpdate(item, true)) Log.Write($"New version registered: {Utils.GetVersionFromName(package.PackageMoniker)}");
                }
            }

            

            Save();
        }

        internal void LoadFiles()
        {
            Log.Write("Loading files...");

            string[] files = Directory.GetFiles(archiveDir);
            foreach (string file in files)
            {
                // Make sure this is an appx
                if (Path.GetExtension(file).ToLower() != ".appx") continue;

                string fileName = Path.GetFileName(file);

                // Construct the new item and add it to the meta
                MetaItem item = new MetaItem(Utils.GetVersionFromName(fileName));
                item.AddFile(fileName);
                if (AddOrUpdate(item, true)) Log.Write($"New version registered: {Utils.GetVersionFromName(fileName)}");
            }

            Save();
        }

        internal async Task LoadVersionDB()
        {
            Log.Write("Loading versiondb...");

            HttpClient client = new HttpClient();

            // Fetch the versiondb
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/MCMrARM/mc-w10-versiondb/master/versions.txt");
            string body = await response.Content.ReadAsStringAsync();

            // Read and process each line
            foreach (string line in body.Split(new[] { '\n' }))
            {
                // Parse the line and get the relevent information
                string[] lineParts = line.Split(new[] { ' ' });

                if (lineParts.Length < 2) continue;

                string updateId = lineParts[0];
                string name = lineParts[1];

                // Ignore EAppx and .70 releases as they are encrypted
                // TODO? Add functionality for this
                if (name.Contains("EAppx") || name.Contains(".70_")) continue;

                name = name.Replace(".Appx", "") + ".Appx";

                // Create the meta and store it
                MetaItem item = new MetaItem(Utils.GetVersionFromName(name));
                item.Archs[Utils.GetArchFromName(name)] = new MetaItemArch(name, new List<Guid>() { Guid.Parse(updateId) });
                if (AddOrUpdate(item, true)) Log.Write($"New version registered: {Utils.GetVersionFromName(name)}");
            }

            Save();
        }
        #endregion

        #region Checks
        internal void CheckMeta()
        {
            Log.Write("Checking for missing meta...");
            // TODO: Add more checks. Is there anything else we need to check?
            foreach (MetaItem item in metaItems.Values)
            {
                foreach (string arch in item.Archs.Keys)
                {
                    if (item.Archs[arch].UpdateIds.Count == 0) Log.WriteWarn($"{item.Version} {arch} missing update ids");
                }
            }
        }

        internal async Task CheckFiles(string token)
        {
            Log.Write("Checking for missing files...");
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += Utils.DownloadProgressChanged;

            foreach (MetaItem item in metaItems.Values)
            {
                foreach (MetaItemArch arch in item.Archs.Values)
                {
                    string outPath = Path.Join(archiveDir, arch.FileName);
                    if (!File.Exists(outPath))
                    {
                        Log.Write($"Downloading {arch.FileName}");

                        List<string> updateIds = arch.UpdateIds.Select(guid => guid.ToString()).ToList();
                        List<string> revisionIds = new List<string>();

                        // Create the revisionId list (all 1 since MC only uses that) and then fetch the urls
                        revisionIds.AddRange(Enumerable.Repeat("1", updateIds.Count));
                        IList<Uri> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds, $"<User>{token}</User>");
                        bool success = false;
                        foreach (Uri uri in Files)
                        {
                            // Check if there is a download link for the file
                            if (uri.Host == "test.com") continue;

                            try
                            {
                                await wc.DownloadFileTaskAsync(uri, outPath);
                                Log.Write();
                                success = true;
                            }
                            catch (WebException ex)
                            {
                                // The download threw an exception so let the user know and cleanup
                                Log.Write();
                                Log.WriteError($"Failed to download: {ex.Message}");
                                File.Delete(outPath);
                            }

                            if (success) break;
                        }

                        if (!success) Log.WriteError($"Failed to download from any urls");
                    }
                }
            }
        }
        #endregion

        #region Accessors
        internal bool AddOrUpdate(MetaItem item, bool skipSave = false)
        {
            bool added = metaItems.TryAdd(item.Version, item);
            if (!added)
            {
                metaItems[item.Version] = item.Merge(metaItems[item.Version]);
            }

            if (!skipSave)
            {
                Save();
            }

            return added;
        }

        internal MetaItem Get(string version)
        {
            return metaItems[version];
        }
        #endregion

        #region Internal
        private void Save()
        {
            using (StreamWriter file = File.CreateText(archiveMetaFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
                serializer.Serialize(file, metaItems);
            }
        }
        #endregion
    }
}
