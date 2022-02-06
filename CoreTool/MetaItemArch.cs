using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class MetaItemArch
    {
        public MetaItemArch() { }

        public MetaItemArch(string fileName)
        {
            FileName = fileName;
            UpdateIds = new List<Guid>();
        }

        public MetaItemArch(string fileName, List<Guid> updateIds)
        {
            FileName = fileName;
            UpdateIds = updateIds;
        }

        public string FileName { get; set; }
        public List<Guid> UpdateIds { get; set; }

        internal MetaItemArch Merge(MetaItemArch metaItemArch)
        {
            MetaItemArch newItem = new MetaItemArch(this.FileName == "" ? metaItemArch.FileName : this.FileName, this.UpdateIds);

            newItem.UpdateIds.AddRange(metaItemArch.UpdateIds);
            newItem.UpdateIds = newItem.UpdateIds.Distinct().ToList();

            return newItem;
        }
    }
}
