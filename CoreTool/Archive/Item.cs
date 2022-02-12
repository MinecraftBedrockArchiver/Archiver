using System.Collections.Generic;
using System.Linq;

namespace CoreTool.Archive
{
    internal class Item
    {
        public string Version { get; set; }
        public SortedDictionary<string, Arch> Archs { get; set; }

        public Item() { }

        public Item(string version)
        {
            Version = version;
            Archs = new SortedDictionary<string, Arch>();
        }

        public Item(string version, SortedDictionary<string, Arch> archs)
        {
            Version = version;
            Archs = archs;
        }

        internal void AddFile(string fileName)
        {
            string arch = Utils.GetArchFromName(fileName);
            if (!Archs.ContainsKey(arch))
            {
                Archs.Add(arch, new Arch(fileName));
            }
        }

        internal Item Merge(Item metaItem)
        {
            Item newItem = new Item(this.Version);

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
