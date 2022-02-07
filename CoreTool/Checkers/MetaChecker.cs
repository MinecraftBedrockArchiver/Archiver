using System.Threading.Tasks;

namespace CoreTool.Checkers
{
    internal class MetaChecker : IChecker
    {
        public Task Check(ArchiveMeta archive)
        {
            archive.Logger.Write("Checking for missing meta...");
            // TODO: Add more checks. Is there anything else we need to check?
            foreach (MetaItem item in archive.GetItems())
            {
                foreach (string arch in item.Archs.Keys)
                {
                    if (item.Archs[arch].UpdateIds.Count == 0) archive.Logger.WriteWarn($"{item.Version} {arch} missing update ids");
                }
            }

            return Utils.CompletedTask;
        }
    }
}
