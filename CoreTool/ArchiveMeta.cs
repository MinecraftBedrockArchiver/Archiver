using CoreTool.Checkers;
using CoreTool.Loaders;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class ArchiveMeta
    {
        public string ArchiveDir { get; private set; }
        public Log Logger { get; internal set; }

        private string name;
        private string archiveMetaFile;

        private Dictionary<string, MetaItem> metaItems;

        private List<ILoader> loaders;
        private List<IChecker> checkers;

        public ArchiveMeta(string name, string archiveDir) : this(name, archiveDir, new List<ILoader>()) { }

        public ArchiveMeta(string name, string archiveDir, List<ILoader> loaders) : this(name, archiveDir, loaders, new List<IChecker>()) { }

        public ArchiveMeta(string name, string archiveDir, List<ILoader> loaders, List<IChecker> checkers)
        {
            this.name = name;
            this.Logger = new Log(name);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(archiveDir))
            {
                Logger.WriteWarn("Created new archive dir: " + archiveDir);
                Directory.CreateDirectory(archiveDir);
            }

            this.ArchiveDir = archiveDir;
            this.archiveMetaFile = Path.Join(archiveDir, "archive_meta.json");

            this.loaders = loaders;
            this.checkers = checkers;

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

        internal async Task Load()
        {
            // Run each loader
            foreach (ILoader loader in this.loaders)
            {
                await loader.Load(this);
            }

            // Save any changes out to file
            Save();
        }

        internal async Task Check()
        {
            // Run each checker
            foreach (IChecker checker in this.checkers)
            {
                await checker.Check(this);
            }
        }

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

        internal MetaItem Get(string version) => metaItems[version];

        internal ReadOnlyCollection<MetaItem> GetItems() => new ReadOnlyCollection<MetaItem>(metaItems.Values.ToList());

        internal string GetToken() => Authentication.GetWUToken();

        internal string GetPrefix() => $"[{name}] ";
        #endregion
        
        /// <summary>
        /// Update the current line with the download progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Logger.WriteRaw("\r{0}%", e.ProgressPercentage);
        }

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
