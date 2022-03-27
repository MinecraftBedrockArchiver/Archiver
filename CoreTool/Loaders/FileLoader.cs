using CoreTool.Archive;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreTool.Loaders
{
    internal class FileLoader : ILoader
    {
        private List<string> fileExts;

        public FileLoader()
        {
            this.fileExts = new List<string> { "appx" };
        }

        public FileLoader(params string[] fileExts)
        {
            this.fileExts = fileExts.ToList();
        }

        public Task Load(ArchiveMeta archive)
        {
            archive.Logger.Write("Loading files...");

            string[] files = Directory.GetFiles(archive.ArchiveDir);
            foreach (string file in files)
            {
                // Make sure this is an allowed ext
                if (!fileExts.Contains(Path.GetExtension(file).ToLower().Substring(1))) continue;

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
