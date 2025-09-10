using CoreTool.Archive;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreTool.Loaders.Windows
{
	internal class XboxLoader : ILoader
	{
		public string PackageId { get; private set; }
		public string PackageName { get; private set; }

		public XboxLoader(string packageId, string packageName)
		{
			this.PackageId = packageId;
			this.PackageName = packageName;
		}

		public async Task Load(ArchiveMeta archive)
		{
			archive.Logger.Write("Loading xbox...");

			HttpClient httpClient = new HttpClient();

			string contentId = await Utils.GetPackageContentId(this.PackageId);
			if (contentId == null) return;

			JsonElement json = await httpClient.GetJsonAsync($"https://packagespc.xboxlive.com/GetBasePackage/{contentId}", await XboxAuth.GetXboxToken());

			// Check if the package was found
			if (json.GetProperty("PackageFound").GetBoolean() == false)
			{
				archive.Logger.WriteError("Package not found");
				return;
			}

			// Get the cdn roots
			//List<string> cdnRoots = new();
			//foreach (var cdnRoot in json.GetProperty("PackageMetadata").GetProperty("CdnRoots").EnumerateArray())
			//{
			//	cdnRoots.Add(cdnRoot.GetString());
			//}

			var versionId = json.GetProperty("VersionId").GetString();

			// Loop through the files and find the msixvc
			foreach (var file in json.GetProperty("PackageMetadata").GetProperty("Files").EnumerateArray())
			{
				string fullPackageName = file.GetProperty("Name").GetString();
				if (!fullPackageName.EndsWith(".msixvc")) continue;

				//var relativeUrl = file.GetProperty("RelativeUrl").GetString();

				//// Create the download urls
				//List<string> downloadUrls = new List<string>();
				//foreach (var cdnRoot in cdnRoots)
				//{
				//	downloadUrls.Add(cdnRoot + relativeUrl);
				//}

				// Create the meta and store it
				Item item = new Item(Utils.GetVersionFromName(fullPackageName));
				item.Archs[Utils.GetArchFromName(fullPackageName)] = new Arch(fullPackageName, new List<string> { versionId });
				if (archive.AddOrUpdate(item, true)) archive.Logger.Write($"New version registered: {Utils.GetVersionFromName(fullPackageName)}");

				break;
			}
		}
	}
}
