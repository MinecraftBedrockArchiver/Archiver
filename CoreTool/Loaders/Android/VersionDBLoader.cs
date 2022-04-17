using CoreTool.Archive;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreTool.Loaders.Android
{
    internal class VersionDBLoader : ILoader
    {
        public async Task Load(ArchiveMeta archive)
        {
            archive.Logger.Write("Loading versiondb...");

            HttpClient client = new HttpClient();

            // Fetch the versiondb
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/minecraft-linux/mcpelauncher-versiondb/master/versions.json");
            List<AndroidVersionDBEntry> versionList = JsonConvert.DeserializeObject<List<AndroidVersionDBEntry>>(await response.Content.ReadAsStringAsync());

            foreach (AndroidVersionDBEntry entry in versionList)
            {
                foreach (string arch in entry.codes.Keys)
                {
                    int versionCode = entry.codes[arch];

                    // Create the predicted filename
                    string fileName = $"com.mojang.minecraftpe-{entry.version_name}-{arch}.apk";

                    // Check if an apks file exists and use that instead
                    if (File.Exists(Path.Join(archive.ArchiveDir, fileName + "s")))
                    {
                        fileName += "s";
                    }

                    // Create the meta and store it
                    Item item = new Item(entry.version_name);
                    item.Archs[arch] = new Arch(fileName, new List<string>() { versionCode.ToString() });
                    if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {entry.version_name}");
                }
            }
        }

        private class AndroidVersionDBEntry
        {
            public string version_name { get; set; }
            public Dictionary<string, int> codes { get; set; }
        }
    }
}
