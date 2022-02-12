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
        private static ConfigData _config;

        public static ConfigData Config => _config ??= Load();

        private static ConfigData Load()
        {
            Utils.GenericLogger.Write("Loading config from " + configFile);

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException("config.json not found, please recreate/download one!");
            }

            return JsonConvert.DeserializeObject<ConfigData>(File.ReadAllText(configFile));
        }
    }
}
