using CoreTool.Archive;
using GooglePlayApi.Helpers;
using GooglePlayApi.Models;
using GooglePlayApi.Proto;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreTool.Checkers.Android
{
    internal class FileChecker : IChecker
    {
        private string packageName;
        private string arch;
        private string device;

        public FileChecker(string packageName, string arch, string device)
        {
            this.packageName = packageName;
            this.arch = arch;
            this.device = device;
        }

        public async Task Check(ArchiveMeta archive)
        {
            archive.Logger.Write($"Checking for missing files for {arch}...");

            HttpClient httpClient = new HttpClient();
            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += archive.DownloadProgressChanged;

            AuthData authData = null;
            AppDetailsHelper appDetailsHelper = null;
            PurchaseHelper purchaseHelper = null;

            foreach (Archive.Item item in archive.GetItems())
            {
                foreach (Arch arch in item.Archs.Values)
                {
                    if (Utils.GetArchFromName(arch.FileName) != this.arch) continue; // Temp while testing

                    string rawFileName = Path.GetFileNameWithoutExtension(arch.FileName);

                    string outPathApk = Path.Join(archive.ArchiveDir, rawFileName + ".apk");
                    string outPathApks = Path.Join(archive.ArchiveDir, rawFileName + ".apks");

                    if (!File.Exists(outPathApk) && !File.Exists(outPathApks) && arch.UpdateIds.Count >= 1)
                    {
                        // Get auth data if its null
                        if (authData == null)
                        {
                            archive.Logger.Write($"Fetching Google Play authentication information...");
                            authData = await Utils.GetGooglePlayAuthData("AuthData.json", ".\\DeviceProperties\\" + device);

                            appDetailsHelper = new AppDetailsHelper(authData, httpClient);
                            purchaseHelper = new PurchaseHelper(authData, httpClient);
                        }

                        archive.Logger.Write($"Downloading {rawFileName}");

                        DeliveryResponse appDelivery = await purchaseHelper.GetDeliveryResponse(packageName, int.Parse(arch.UpdateIds[0]), 1);

                        // Check if the delivery failed and alert the user
                        if (appDelivery.Status != 1)
                        {
                            string reason = "Unknown";

                            switch (appDelivery.Status)
                            {
                                case 2:
                                    reason = "App not supported!";
                                    break;
                                case 3:
                                    reason = "App not purchased!";
                                    break;
                            }

                            archive.Logger.WriteError($"Failed to download {rawFileName} as delivery returned: {reason} ({appDelivery.Status})");
                            continue;
                        }

                        // Check if the apk is split or not
                        if (appDelivery.AppDeliveryData.SplitDeliveryData.Count == 0)
                        {
                            try
                            {
                                await wc.DownloadFileTaskAsync(appDelivery.AppDeliveryData.DownloadUrl, outPathApk);
                                Console.WriteLine();

                                archive.Logger.WriteWarn("Calculating file hashes, this may take some time");
                                arch.Hashes = new FileHashes(outPathApk);

                                arch.FileName = outPathApk;
                            }
                            catch (WebException ex)
                            {
                                // The download threw an exception so let the user know and cleanup
                                Console.WriteLine();
                                archive.Logger.WriteError($"Failed to download: {ex.Message}");
                                File.Delete(outPathApk);
                            }
                        }
                        else
                        {
                            string apkFolder = "./tmp/" + rawFileName + "/";
                            try
                            {
                                archive.Logger.Write($"{rawFileName} is split, downloading parts to temp...");

                                if (!Directory.Exists(apkFolder))
                                {
                                    Directory.CreateDirectory(apkFolder);
                                }

                                // Download the base apk
                                archive.Logger.Write($"Downloading base.apk");
                                await wc.DownloadFileTaskAsync(appDelivery.AppDeliveryData.DownloadUrl, apkFolder + "base.apk");
                                Console.WriteLine();

                                // Download the split parts
                                foreach (SplitDeliveryData splitDeliveryData in appDelivery.AppDeliveryData.SplitDeliveryData)
                                {
                                    string splitName = "split_" + splitDeliveryData.Name + ".apk";
                                    archive.Logger.Write($"Downloading " + splitName);
                                    await wc.DownloadFileTaskAsync(new Uri(splitDeliveryData.DownloadUrl), apkFolder + splitName);
                                    Console.WriteLine();
                                }

                                // Zip the split apk parts into the archive
                                archive.Logger.Write($"Merging split apks...");
                                ZipFile.CreateFromDirectory(apkFolder, outPathApks);

                                Directory.Delete(apkFolder, true);

                                archive.Logger.WriteWarn("Calculating file hashes, this may take some time");
                                arch.Hashes = new FileHashes(outPathApks);

                                arch.FileName = outPathApks;
                            }
                            catch (WebException ex)
                            {
                                // The download threw an exception so let the user know and cleanup
                                Console.WriteLine();
                                archive.Logger.WriteError($"Failed to download: {ex.Message}");

                                if (Directory.Exists(apkFolder))
                                {
                                    Directory.Delete(apkFolder, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
