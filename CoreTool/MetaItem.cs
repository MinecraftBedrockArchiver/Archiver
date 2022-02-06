using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class MetaItem
    {
        public MetaItem() { }

        public MetaItem(string version)
        {
            Version = version;
            Archs = new Dictionary<string, MetaItemArch>();
        }

        public MetaItem(string version, Dictionary<string, MetaItemArch> archs)
        {
            Version = version;
            Archs = archs;
        }

        public string Version { get; set; }
        public Dictionary<string, MetaItemArch> Archs { get; set; }

        internal void AddFile(string fileName)
        {
            string arch = Utils.GetArchFromName(fileName);
            if (!Archs.ContainsKey(arch))
            {
                Archs.Add(arch, new MetaItemArch(fileName));
            }
        }

        internal MetaItem Merge(MetaItem metaItem)
        {
            MetaItem newItem = new MetaItem(this.Version);

            foreach(string arch in this.Archs.Keys.Concat(metaItem.Archs.Keys).Distinct())
            {
                if (this.Archs.ContainsKey(arch) && metaItem.Archs.ContainsKey(arch))
                {
                    newItem.Archs.Add(arch, this.Archs[arch].Merge(metaItem.Archs[arch]));
                }
                else
                {
                    if (this.Archs.ContainsKey(arch)) newItem.Archs.Add(arch, this.Archs[arch]);
                    if (metaItem.Archs.ContainsKey(arch)) newItem.Archs.Add(arch, metaItem.Archs[arch]);
                }
            }

            return newItem;
        }
    }
}
