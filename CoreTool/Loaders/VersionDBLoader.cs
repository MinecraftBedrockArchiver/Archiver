using CoreTool.Archive;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreTool.Loaders
{
    internal class VersionDBLoader : ILoader
    {
        private string packageName;

        public VersionDBLoader(string packageName)
        {
            this.packageName = packageName;
        }

        public async Task Load(ArchiveMeta archive)
        {
            archive.Logger.Write("Loading versiondb...");

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

                // Make sure the entry is for this package
                if (!name.StartsWith(packageName + "_")) continue;

                name = name.Replace(".Appx", "") + ".Appx";

                // Create the meta and store it
                Item item = new Item(Utils.GetVersionFromName(name));
                item.Archs[Utils.GetArchFromName(name)] = new Arch(name, new List<Guid>() { Guid.Parse(updateId) });
                if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(name)}");
            }
        }
    }
}
