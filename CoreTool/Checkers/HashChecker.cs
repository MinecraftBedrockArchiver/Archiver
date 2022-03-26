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

                        
                    if (string.IsNullOrEmpty(arch.Hashes.MD5))
                    {
                        archive.Logger.WriteWarn($"Missing MD5 hash for {arch.FileName}, calculating...");
                        arch.Hashes.CalculateMD5(filePath);
                        hasChanges = true;
                    }

                    if (string.IsNullOrEmpty(arch.Hashes.SHA256))
                    {
                        archive.Logger.WriteWarn($"Missing SHA256 hash for {arch.FileName}, calculating...");
                        arch.Hashes.CalculateSHA256(filePath);
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
