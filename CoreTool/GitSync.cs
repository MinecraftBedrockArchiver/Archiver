using CoreTool.Archive;
using CoreTool.Config;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class GitSync
    {
        private GitSyncEntry Config => Loader.Config.GitSync;
        public Log Logger { get; internal set; }

        public GitSync()
        {
            this.Logger = new Log("GitSync");
        }

        public async Task Load()
        {
            Logger.Write("Initialising GitSync");

            // Make sure git is accessible
            try
            {
                await Utils.RunProcessAsync("git.exe");
            }
            catch (Win32Exception)
            {
                Logger.WriteError("Git not installed disabling sync");
                Config.Enabled = false;
                return;
            }

            if (!Directory.Exists(Config.Folder))
            {
                Logger.Write("Directory missing, cloning repo");
                await Utils.RunProcessAsync("git.exe", $"clone {Config.Repo} {Config.Folder}");
            }

            Logger.Write("Pulling any updates");
            await Utils.RunProcessAsync("git.exe", $"pull", Config.Folder);
        }

        public async Task Check()
        {
            Logger.Write("Collating archive meta files");
            foreach (ArchiveMeta archive in Loader.Config.ArchiveInstances)
            {
                File.Copy(archive.ArchiveMetaFile, Path.Combine(Config.Folder, archive.Name.ToLower().Replace(' ', '_') + "_meta.json"), true);
            }

            Logger.Write("Checking for changes");
            if ((await Utils.RunProcessAsync("git.exe", $"status --porcelain", Config.Folder)).Output != "")
            {
                Logger.Write("Changes found commiting and pushing");
                await Utils.RunProcessAsync("git.exe", $"add .", Config.Folder);
                await Utils.RunProcessAsync("git.exe", $"commit -m \"Updated meta files\"", Config.Folder);
                await Utils.RunProcessAsync("git.exe", $"push", Config.Folder);
                Logger.Write("Pushed changes successfully");
            }
        }
    }
}
