using CoreTool.Archive;
using System.IO;
using System.Threading.Tasks;

namespace CoreTool.Checkers
{
    internal class HashChecker : IChecker
    {
        public Task Check(ArchiveMeta archive)
        {
            archive.Logger.Write("Checking for missing file hashes...");

            bool hasChanges = false;

            foreach (Item item in archive.GetItems())
            {
                foreach (Arch arch in item.Archs.Values)
                {
                    string filePath = Path.Join(archive.ArchiveDir, arch.FileName);

                    if (!File.Exists(filePath)) continue;

                        
                    if (arch.Hashes.HasMissing())
                    {
                        archive.Logger.WriteWarn($"Missing one or more hashes for {arch.FileName}, calculating...");
                        arch.Hashes.CalculateHashes(filePath);
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                archive.Save();
            }

            return Utils.CompletedTask;
        }
    }
}
