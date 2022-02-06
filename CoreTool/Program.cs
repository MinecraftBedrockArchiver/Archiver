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

            string token = Authentication.GetWUToken();

            Console.WriteLine($"Got token: {token}");

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

            // Set the download dir
            // TODO: Make this configurable
            string downloadDir = @"\\192.168.1.5\Archive\Minecraft\Windows10 - Microsoft.MinecraftUWP_8wekyb3d8bbwe\";

            Console.WriteLine("Fetching versionsdb");

            // Fetch the versiondb
            HttpResponseMessage response = await client.GetAsync("https://raw.githubusercontent.com/MCMrARM/mc-w10-versiondb/master/versions.txt");
            string body = await response.Content.ReadAsStringAsync();

            // Read and process each line
            foreach (string line in body.Split(new[] { '\n' }))
            {
                // Parse the line and get the relevent information
                string[] lineParts = line.Split(new[] { ' ' });
                
                if (lineParts.Length < 2) continue;

                string updateId = lineParts[0];
                string name = lineParts[1];

                // Ignore EAppx and .70 releases as they are encrypted
                // TODO? Add functionality for this
                if (name.Contains("EAppx") || name.Contains(".70_")) continue;

                name = name.Replace(".Appx", "");

                // Check the file doesnt already exist
                string outPath = Path.Join(downloadDir, name + ".Appx");
                if (File.Exists(outPath))
                {
                    Console.WriteLine(name + " - Already downloaded!");
                    continue;
                }

                updateIds.Add(updateId);
                names.Add(name);
            }

            Console.WriteLine($"Found {updateIds.Count} versions, getting urls...");

            // Create the revisionId list (all 1 since MC only uses that) and then fetch the urls
            revisionIds.AddRange(Enumerable.Repeat("1", updateIds.Count));
            IList<Uri> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds, $"<User>{token}</User>");

            Console.WriteLine("Downloading versions");

            int i = 0;
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += Wc_DownloadProgressChanged;
            foreach (Uri uri in Files)
            {
                // Check if there is a download link for the file
                if (uri.Host == "test.com")
                {
                    i++;
                    continue;
                }

                // Download the file
                Console.WriteLine($"Downloading {names[i]} ({updateIds[i]})");
                string outPath = Path.Join(downloadDir, names[i] + ".Appx");
                try
                {
                    await wc.DownloadFileTaskAsync(uri, outPath);
                    Console.WriteLine();
                }
                catch (WebException ex)
                {
                    // The download threw an exception so let the user know and cleanup
                    Console.WriteLine();
                    Console.WriteLine($"Failed to download: {ex.Message}");
                    File.Delete(outPath);
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
            // Create a packages var for debugging
            IList<PackageInstance> packages;

            // Make sure we have a token, if not don't bother checking for betas
            if (token == "")
            {
                Console.WriteLine("Failed to get token! Unable to fetch beta.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Fetching Beta... (if this returns release versions then you need to opt into the beta)");

                // Grab the packages for the beta using auth
                await dcathandler.QueryDCATAsync("9NBLGGH2JHXJ", IdentiferType.ProductID, "Bearer WLID1.0=" + Convert.FromBase64String(token));
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

            // Grab the packages for the release
            await dcathandler.QueryDCATAsync("9NBLGGH2JHXJ");
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
