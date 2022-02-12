using CoreTool.Archive;
using CoreTool.Checkers;
using CoreTool.Loaders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CoreTool.Config
{
    internal class ClassEntry<T>
    {
        public string Class { get; set; }
        public List<object> Params { get; set; }

        public T Create()
        {
            Type type = Type.GetType(Class);
            return (T)Activator.CreateInstance(type, BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance | BindingFlags.OptionalParamBinding, null, Params?.ToArray(), CultureInfo.CurrentCulture);
        }
    }

    internal class ArchiveEntry
    {
        public string Name { get; set; }
        public string Directory { get; set; }
        public List<ClassEntry<ILoader>> Loaders { get; set; }
        public List<ClassEntry<IChecker>> Checkers { get; set; }

        public ArchiveMeta Create()
        {
            return new ArchiveMeta(Name, Directory, Loaders?.Select(x => x.Create()).ToList(), Checkers?.Select(x => x.Create()).ToList());
        }
    }

    internal class GitSyncEntry
    {
        public bool Enabled { get; set; }
        public string Repo { get; set; }
        public string Folder { get; set; }
    }

    internal class ConfigData
    {
        public GitSyncEntry GitSync { get; set; }
        public List<ArchiveEntry> Archives { get; set; }

        [JsonIgnore]
        private List<ArchiveMeta> _archiveInstances;

        [JsonIgnore]
        public ReadOnlyCollection<ArchiveMeta> ArchiveInstances => (_archiveInstances ??= Archives.Select(x => x.Create()).ToList()).AsReadOnly();
    }

}
