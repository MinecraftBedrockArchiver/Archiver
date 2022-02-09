using System.Collections.Generic;
using System.Linq;

namespace CoreTool.Archive
{
    internal class Item
    {
        public Item() { }

        public Item(string version)
        {
            Version = version;
            Archs = new Dictionary<string, Arch>();
        }

        public Item(string version, Dictionary<string, Arch> archs)
        {
            Version = version;
            Archs = archs;
        }

        public string Version { get; set; }
        public Dictionary<string, Arch> Archs { get; set; }

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
