using CoreTool.Checkers;
using CoreTool.Loaders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreTool.Archive
{
    internal class ArchiveMeta
    {
        public string ArchiveDir { get; private set; }
        public Log Logger { get; internal set; }
        public string Name { get; internal set; }
        public string ArchiveMetaFile { get; internal set; }
        public string? PackageId { get; internal set; }

		private SortedDictionary<string, Item> metaItems;

        private List<ILoader> loaders;
        private List<IChecker> checkers;

        public ArchiveMeta(string name, string archiveDir) : this(name, archiveDir, new List<ILoader>()) { }

        public ArchiveMeta(string name, string archiveDir, List<ILoader> loaders) : this(name, archiveDir, loaders, new List<IChecker>()) { }

        public ArchiveMeta(string name, string archiveDir, List<ILoader> loaders, List<IChecker> checkers)
        {
            this.Name = name;
            this.Logger = new Log(name);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(archiveDir))
            {
                try
                {
                    Directory.CreateDirectory(archiveDir);
                    Logger.WriteWarn("Created new archive dir: " + archiveDir);
                }
                catch (IOException ex)
                {
                    Logger.WriteError("Unable to create archive directory: " + ex.Message);
                    Environment.Exit(1);
                }
            }

            this.ArchiveDir = archiveDir;
            this.ArchiveMetaFile = Path.Join(archiveDir, "archive_meta.json");

            this.loaders = loaders;
            this.checkers = checkers;

			// Locate package id if we have a store loader
            foreach (ILoader loader in loaders)
            {
                if (loader is Loaders.Windows.StoreLoader storeLoader)
                {
                    this.PackageId = storeLoader.PackageId;
                }
                else if (loader is Loaders.Windows.XboxLoader xboxLoader)
                {
                    this.PackageId = xboxLoader.PackageId;
                }
			}

			metaItems = new SortedDictionary<string, Item>(new VersionComparer());

            // Load the meta or create a new one
            if (File.Exists(ArchiveMetaFile))
            {
                // We have to do it like this so we use the correct comparer
                SortedDictionary<string, Item> loadedMetaItems = JsonConvert.DeserializeObject<SortedDictionary<string, Item>>(File.ReadAllText(ArchiveMetaFile));
                foreach(var item in loadedMetaItems)
                {
                    metaItems[item.Key] = item.Value;
                }
            }
            else
            {
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
        internal bool AddOrUpdate(Item item, bool skipSave = false)
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

        internal Item Get(string version) => metaItems[version];

        internal ReadOnlyCollection<Item> GetItems() => new ReadOnlyCollection<Item>(metaItems.Values.ToList());

        internal string GetPrefix() => $"[{Name}] ";
        #endregion
        
        /// <summary>
        /// Update the current line with the download progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void DownloadProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Logger.WriteRaw("\r{0}%", e.ProgressPercentage);
        }

        internal void Save()
        {
            using (StreamWriter file = File.CreateText(ArchiveMetaFile))
            {
                JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings() { Formatting = Formatting.Indented });
                serializer.Serialize(file, metaItems);
            }
        }
    }
}
