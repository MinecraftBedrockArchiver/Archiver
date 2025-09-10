using CoreTool.Archive;
using StoreLib.Models;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreTool.Checkers.Windows
{
    internal class FileChecker : IChecker
    {
        public async Task Check(ArchiveMeta archive)
        {
            archive.Logger.Write("Checking for missing files...");

            HttpClient httpClient = new HttpClient();

            bool hasChanges = false;

            foreach (Item item in archive.GetItems())
            {
                foreach (Arch arch in item.Archs.Values)
                {
                    string outPath = Path.Join(archive.ArchiveDir, arch.FileName);
                    if (!File.Exists(outPath))
                    {
                        archive.Logger.Write($"Downloading {arch.FileName}");

						if (arch.FileName.EndsWith(".msixvc"))
						{
							hasChanges |= await DownloadXbox(arch, httpClient, outPath, archive);
						}
						else
						{
							hasChanges |= await DownloadStore(arch, httpClient, outPath, archive);
						}
					}
                }
            }

            if (hasChanges)
            {
                archive.Save();
            }
        }

		private async Task<bool> DownloadStore(Arch arch, HttpClient httpClient, string outPath, ArchiveMeta archive)
        {
			bool hasChanges = false;

			List<string> updateIds = arch.UpdateIds.Select(guid => guid.ToString()).ToList();
			List<string> revisionIds = new List<string>();

			// Create the revisionId list (all 1 since MC only uses that) and then fetch the urls
			revisionIds.AddRange(Enumerable.Repeat("1", updateIds.Count));
			IList<PackageFileInfo> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds);
			bool success = false;
			foreach (PackageFileInfo file in Files)
			{
				// Check if there is a download link for the file
				if (file.Uri == null) continue;

				try
				{
					await httpClient.DownloadFileTaskAsync(file.Uri, outPath, archive.DownloadProgressChanged);
					Console.WriteLine();
					archive.Logger.WriteWarn("Calculating file hashes, this may take some time");
					arch.Hashes = new FileHashes(outPath);

					if (arch.Hashes.SHA1 != file.Hash)
					{
						throw new Exception("File hash does not match");
					}

					success = true;
					hasChanges = true;
				}
				catch (Exception ex)
				{
					// The download threw an exception so let the user know and cleanup
					Console.WriteLine();
					archive.Logger.WriteError($"Failed to download: {ex.Message}");
					File.Delete(outPath);
				}

				if (success) break;
			}

			if (!success) archive.Logger.WriteError($"Failed to download from any update ids");

			return hasChanges;
		}

		private async Task<bool> DownloadXbox(Arch arch, HttpClient httpClient, string outPath, ArchiveMeta archive)
		{
			bool hasChanges = false;

			string contentId = await Utils.GetPackageContentId(archive.PackageId);
			if (contentId == null) return hasChanges;
			
			bool success = false;
			foreach (string updateId in arch.UpdateIds)
			{
				JsonElement json = await httpClient.GetJsonAsync($"https://packagespc.xboxlive.com/GetSpecificBasePackage/{contentId}/{updateId}", await XboxAuth.GetXboxToken());

				// Check if the package was found
				if (json.GetProperty("PackageFound").GetBoolean() == false)
				{
					archive.Logger.WriteError("Package not found");
					break;
				}

				// Get the cdn root
				string cdnRoot = json.GetProperty("PackageMetadata").GetProperty("CdnRoots").EnumerateArray().First().GetString();

				var downloadUrl = "";

				// Loop through the files and find the msixvc
				foreach (var file in json.GetProperty("PackageMetadata").GetProperty("Files").EnumerateArray())
				{
					string fullPackageName = file.GetProperty("Name").GetString();
					if (!fullPackageName.EndsWith(".msixvc")) continue;

					var relativeUrl = file.GetProperty("RelativeUrl").GetString();

					downloadUrl = cdnRoot + relativeUrl;

					break;
				}

				if (string.IsNullOrEmpty(downloadUrl))
				{
					continue;
				}

				// Download the file
				try
				{
					await httpClient.DownloadFileTaskAsync(downloadUrl, outPath, archive.DownloadProgressChanged);
					Console.WriteLine();
					archive.Logger.WriteWarn("Calculating file hashes, this may take some time");
					arch.Hashes = new FileHashes(outPath);

					// TODO Check

					success = true;
					hasChanges = true;
				}
				catch (Exception ex)
				{
					// The download threw an exception so let the user know and cleanup
					Console.WriteLine();
					archive.Logger.WriteError($"Failed to download: {ex.Message}");
					File.Delete(outPath);
				}

				if (success) break;
			}

			if (!success) archive.Logger.WriteError($"Failed to download from any update ids");

			return hasChanges;
		}
	}
}
