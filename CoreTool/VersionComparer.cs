using System.Collections.Generic;

namespace CoreTool
{
    internal class VersionComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return Utils.StrCmpLogicalW(x, y);
        }
    }
}
