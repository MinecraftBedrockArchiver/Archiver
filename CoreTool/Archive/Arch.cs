using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreTool.Archive
{
    internal class Arch
    {
        public Arch() { }

        public Arch(string fileName)
        {
            FileName = fileName;
            UpdateIds = new List<Guid>();
        }

        public Arch(string fileName, List<Guid> updateIds)
        {
            FileName = fileName;
            UpdateIds = updateIds;
        }

        public string FileName { get; set; }
        public List<Guid> UpdateIds { get; set; }

        internal Arch Merge(Arch metaItemArch)
        {
            Arch newItem = new Arch(this.FileName == "" ? metaItemArch.FileName : this.FileName, this.UpdateIds);

            newItem.UpdateIds.AddRange(metaItemArch.UpdateIds);
            newItem.UpdateIds = newItem.UpdateIds.Distinct().ToList();

            return newItem;
        }
    }
}
