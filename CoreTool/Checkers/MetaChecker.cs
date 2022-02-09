using CoreTool.Archive;
using System.Linq;
using System.Threading.Tasks;

namespace CoreTool.Checkers
{
    internal class MetaChecker : IChecker
    {
        public Task Check(ArchiveMeta archive)
        {
            archive.Logger.Write("Checking for missing meta...");
            // TODO: Add more checks. Is there anything else we need to check?
            foreach (Item item in archive.GetItems())
            {
                // Check if the version stored is wrong based on the filename
                // This shouldn't ever happen
                string fileVersion = Utils.GetVersionFromName(item.Archs.Values.First().FileName);
                if (item.Version != fileVersion) archive.Logger.WriteWarn($"{item.Version} is incorrectly stored, should be {fileVersion}");

                foreach (string arch in item.Archs.Keys)
                {
                    if (item.Archs[arch].UpdateIds.Count == 0) archive.Logger.WriteWarn($"{item.Version} {arch} missing update ids");
                }
            }

            return Utils.CompletedTask;
        }
    }
}
