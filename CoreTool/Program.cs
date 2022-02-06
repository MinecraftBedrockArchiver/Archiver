using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using StoreLib.Models;
using StoreLib.Services;

namespace MinecraftW10Downloader
{
    class Program
    {

        static async Task Main(string[] args)
        {
            DisplayCatalogHandler dcathandler = DisplayCatalogHandler.ProductionConfig();

            Console.WriteLine("Getting token...");

            string token = await Authentication.GetWUToken();

            await DownloadKnownVersions(token);

            await GetLatestVersions(dcathandler, token);

            Console.ReadLine();
        }

        /// <summary>
        /// Download every version listed in MCMrARMs versiondb
        /// </summary>
        /// <param name="token">The MSA token for downloading the betas</param>
        /// <returns></returns>
        private static async Task DownloadKnownVersions(string token)
        {
            HttpClient client = new HttpClient();
            List<string> updateIds = new List<string>();
            List<string> revisionIds = new List<string>();
            List<string> names = new List<string>();

            string downloadDir = @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\";

            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/MCMrARM/mc-w10-versiondb/master/versions.txt");

            string body = await response.Content.ReadAsStringAsync();

            foreach (string line in body.Split(new[] { '\n' }))
            {
                string[] lineParts = line.Split(new[] { ' ' });
                
                if (lineParts.Length < 2) continue;

                string updateId = lineParts[0];
                string name = lineParts[1];

                if (name.Contains("EAppx") || name.Contains(".70_")) continue;

                name = name.Replace(".EAppx", "").Replace(".Appx", "");

                updateIds.Add(updateId);
                names.Add(name);
            }

            revisionIds.AddRange(Enumerable.Repeat("1", updateIds.Count));
            IList<Uri> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds, $"<User>{token}</User>");

            int i = 0;
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            foreach (Uri uri in Files)
            {
                if (uri.Host == "test.com")
                {
                    i++;
                    continue;
                }

                Console.WriteLine($"Downloading {names[i]} ({updateIds[i]})");
                string outPath = Path.Join(downloadDir, names[i] + ".Appx");
                if (File.Exists(outPath))
                {
                    Console.WriteLine("File already downloaded!");
                }
                else
                {
                    await wc.DownloadFileTaskAsync(uri, outPath);
                    Console.WriteLine();
                }

                i++;
            }
        }

        /// <summary>
        /// Update the current line with the download progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write("\r{0}%", e.ProgressPercentage);
        }

        /// <summary>
        /// Get the latest beta and release versions from the store
        /// </summary>
        /// <param name="dcathandler"></param>
        /// <param name="token">The MSA token for fetching the betas</param>
        /// <returns></returns>
        private static async Task GetLatestVersions(DisplayCatalogHandler dcathandler, string token)
        {
            IList<PackageInstance> packages;
            if (token == "")
            {
                Console.WriteLine("Failed to get token! Unable to fetch beta.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Fetching Beta... (if this returns release versions then you need to opt into the beta)");

                await dcathandler.QueryDCATAsync("9nblggh2jhxj", IdentiferType.ProductID, "Bearer WLID1.0=" + Convert.FromBase64String(token));
                packages = await dcathandler.GetPackagesForProductAsync($"<User>{token}</User>");
                foreach (PackageInstance package in packages)
                {
                    if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                    if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                    Console.WriteLine("{0}\t{1}\t{2}", package.PackageMoniker, package.UpdateId, package.PackageUri);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Fetching Release...");

            await dcathandler.QueryDCATAsync("9nblggh2jhxj");
            packages = await dcathandler.GetPackagesForProductAsync();
            foreach (PackageInstance package in packages)
            {
                if (!package.PackageMoniker.StartsWith("Microsoft.MinecraftUWP_")) continue;
                if (package.ApplicabilityBlob.ContentTargetPlatforms[0].PlatformTarget != 0) continue;

                Console.WriteLine("{0}\t{1}\t{2}", package.PackageMoniker, package.UpdateId, package.PackageUri);
            }
        }
    }
}
