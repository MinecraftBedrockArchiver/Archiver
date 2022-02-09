using CoreTool.Archive;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CoreTool.Config
{
    internal class Loader
    {
        private const string configFile = "config.json";

        public static List<ArchiveMeta> Load()
        {
            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile, "[\n  {\n    \"Name\": \"W10\",\n    \"Directory\": \".\\\\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\\\\\",\n    \"Loaders\": [\n      {\n      \"Class\": \"CoreTool.Loaders.FileLoader\"\n      },\n      {\n      \"Class\": \"CoreTool.Loaders.VersionDBLoader\"\n      },\n      {\n      \"Class\": \"CoreTool.Loaders.StoreLoader\",\n      \"Params\": [\n        \"9NBLGGH2JHXJ\",\n        \"Microsoft.MinecraftUWP\"\n      ]\n      }\n    ],\n    \"Checkers\": [\n      {\n      \"Class\": \"CoreTool.Checkers.MetaChecker\"\n      },\n      {\n      \"Class\": \"CoreTool.Checkers.FileChecker\"\n      }\n    ]\n  }\n]");
                throw new FileNotFoundException("config.json not found, created a default one for you to fill out");
            }

            List<ArchiveEntry> config = JsonConvert.DeserializeObject<List<ArchiveEntry>>(File.ReadAllText(configFile));

            return config.Select(x => x.Create()).ToList();
        }
    }
}
