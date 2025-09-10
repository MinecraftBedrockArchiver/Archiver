using System.Collections.Generic;
using System.Linq;

namespace CoreTool.Archive
{
    internal class Arch
    {
        public string FileName { get; set; }
        public FileHashes Hashes { get; set; }
        public List<string> UpdateIds { get; set; }

        public Arch() { }

        public Arch(string fileName) : this(fileName, new()) { }

        public Arch(string fileName, List<string> updateIds) : this(fileName, new(), updateIds) { }

        public Arch(string fileName, FileHashes hashes, List<string> updateIds)
        {
            FileName = fileName;
            Hashes = hashes;
            UpdateIds = updateIds;
        }

        internal Arch Merge(Arch metaItemArch)
        {
            Arch newItem = new Arch(this.FileName == "" ? metaItemArch.FileName : this.FileName, this.Hashes.Merge(metaItemArch.Hashes), this.UpdateIds);

            newItem.UpdateIds.AddRange(metaItemArch.UpdateIds);
            newItem.UpdateIds = newItem.UpdateIds.Distinct().ToList();

            return newItem;
        }
    }
}
