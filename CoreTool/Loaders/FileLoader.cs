using CoreTool.Archive;
using System.IO;
using System.Threading.Tasks;

namespace CoreTool.Loaders
{
    internal class FileLoader : ILoader
    {
        public Task Load(ArchiveMeta archive)
        {
            archive.Logger.Write("Loading files...");

            string[] files = Directory.GetFiles(archive.ArchiveDir);
            foreach (string file in files)
            {
                // Make sure this is an appx
                if (Path.GetExtension(file).ToLower() != ".appx") continue;

                string fileName = Path.GetFileName(file);

                // Construct the new item and add it to the meta
                Item item = new Item(Utils.GetVersionFromName(fileName));
                item.AddFile(fileName);
                if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(fileName)}");
            }

            return Utils.CompletedTask;
        }
    }
}
