using Microsoft.Isam.Esent.Interop;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace DataStoreExtractor
{
    internal class Program
    {
        private static List<string> UpdateIds { get; set; }  = new List<string>();

        private static string[] wantedIds = new string[] {
            "9NBLGGH2JHXJ", // Microsoft.MinecraftUWP
	        "9P5X4QVLC2XR", // Microsoft.MinecraftWindowsBeta
            "9NBLGGH537BL", // Microsoft.MinecraftUWPConsole
	        "9MTK992XRFL2" // Microsoft.MinecraftUWPBeta
        };

        static async Task Main(string[] args)
        {
            CopyDatastore();

            CollectFromDatastore(); // Read the datastore properly

            ExtractFromDatatstore(); // Carve the bytes out of the datastore to find any other ids

            await DownloadAndCheck();
        }

        private static void CopyDatastore()
        {
            try
            {
                // Copy the datastore to our current dir
                File.Copy(@"C:\Windows\SoftwareDistribution\DataStore\DataStore.edb", "DataStore.edb", true);
            }
            catch (IOException)
            {
                Console.WriteLine("Unable to copy the DataStore.edb, make sure the Windows Update (wuauserv) service is stopped");
                Environment.Exit(1);
            }
        }

        private static void CollectFromDatastore()
        {
            Dictionary<Guid, string> updatePackageMap = new Dictionary<Guid, string>();

            // Setup the edb reading
            JET_INSTANCE instance;
            JET_SESID sesid;
            JET_DBID dbid;
            JET_TABLEID tableid;
            IDictionary<string, JET_COLUMNID> colMap;

            SystemParameters.DatabasePageSize = 16 * 1024;

            Api.JetCreateInstance(out instance, Guid.NewGuid().ToString());
            Api.JetInit(ref instance);

            Api.JetBeginSession(instance, out sesid, null, null);

            Api.JetAttachDatabase(sesid, @"DataStore.edb", AttachDatabaseGrbit.ReadOnly);

            Api.OpenDatabase(sesid, @"DataStore.edb", out dbid, OpenDatabaseGrbit.ReadOnly);

            // Pull from history
            Api.OpenTable(sesid, dbid, "tbHistory", OpenTableGrbit.None, out tableid);

            colMap = Api.GetColumnDictionary(sesid, tableid);

            do
            {
                string title = Api.RetrieveColumnAsString(sesid, tableid, colMap["Title"]);
                Guid updateId = new Guid(Api.RetrieveColumn(sesid, tableid, colMap["UpdateId"]).Take(16).ToArray());

                if (wantedIds.Contains(title.Split('-')[0]))
                {
                    updatePackageMap[updateId] = title;
                }
            }
            while (Api.TryMoveNext(sesid, tableid));

            // Pull from updates
            Api.OpenTable(sesid, dbid, "tbUpdates", OpenTableGrbit.None, out tableid);

            colMap = Api.GetColumnDictionary(sesid, tableid);

            Dictionary<int, Guid> updateMap = new Dictionary<int, Guid>();

            do
            {
                int idLocal = Api.RetrieveColumnAsInt32(sesid, tableid, colMap["IdLocal"]).Value;
                Guid updateId = new Guid(Api.RetrieveColumn(sesid, tableid, colMap["UpdateId"]).Take(16).ToArray());

                updateMap[idLocal] = updateId;
            }
            while (Api.TryMoveNext(sesid, tableid));

            // Collate the names with the pulled updates
            Api.OpenTable(sesid, dbid, "tbUpdateLocalizedProps", OpenTableGrbit.None, out tableid);

            colMap = Api.GetColumnDictionary(sesid, tableid);

            do
            {
                string title = Api.RetrieveColumnAsString(sesid, tableid, colMap["Title"]);
                int idLocal = Api.RetrieveColumnAsInt32(sesid, tableid, colMap["IdLocal"]).Value;

                if (wantedIds.Contains(title.Split('-')[0]))
                {
                    updatePackageMap[updateMap[idLocal]] = title;
                }
            }
            while (Api.TryMoveNext(sesid, tableid));

            // Tell the user the IDs that we have found
            foreach (var item in updatePackageMap)
            {
                Console.WriteLine($"{item.Value} -> {item.Key}");
            }

            UpdateIds.AddRange(updatePackageMap.Keys.Select(x => x.ToString()).ToList());
        }

        private static void ExtractFromDatatstore()
        {
            byte[] dataStoreBytes = File.ReadAllBytes("./DataStore.edb");
            byte[] pattern = new byte[] { 0x16, 0x20, 0x7F };

            // Find update ids
            for (int i = 0; i < dataStoreBytes.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int k = 0; k < pattern.Length; k++)
                {
                    if (dataStoreBytes[i + k] != pattern[k])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    byte[] tmpId = new byte[16];
                    Array.Copy(dataStoreBytes, i + pattern.Length, tmpId, 0, 16);
                    UpdateIds.Add(new Guid(tmpId).ToString());
                }
            }
        }

        private static async Task DownloadAndCheck()
        {
            // Remove duplicates
            UpdateIds = UpdateIds.Distinct().ToList();

            // Ask the user what they want to do
            Console.WriteLine($"Found {UpdateIds.Count} update ids");
            Console.Write("Do you want to try to download these and check they are valid? ");
            if (Console.ReadKey().KeyChar != 'y')
            {
                Console.WriteLine();
                Environment.Exit(0);
            }

            Console.WriteLine();

            Console.Write("Do you want to delete valid files after download? ");
            bool cleanUp = false;
            if (Console.ReadKey().KeyChar == 'y')
            {
                cleanUp = true;
            }

            Console.WriteLine();

            string downloadFolder = "./Downloads/";
            Directory.CreateDirectory(downloadFolder);

            int chunkSize = 250;

            List<List<string>> updateIdsChunked = UpdateIds
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            Dictionary<string, string> validUpdates = new Dictionary<string, string>();
            int i = 0;
            foreach (List<string> updateIdsChunk in updateIdsChunked) {
                i++;
                Console.WriteLine($"Processing next chunk of {chunkSize} ({i}/{updateIdsChunked.Count}) updates");
                foreach (KeyValuePair<string, string> validUpdate in await DownloadChunk(downloadFolder, cleanUp, updateIdsChunk))
                {
                    validUpdates[validUpdate.Key] = validUpdate.Value;
                }
            }

            // Tell the user the final packages
            Console.WriteLine();
            Console.WriteLine("Finished downloads:");
            string output = "";
            foreach (string update in validUpdates.Keys)
            {
                output += $"{update} -> {validUpdates[update]}\n";
            }
            Console.WriteLine(output);
            File.WriteAllText(Path.Join(downloadFolder, "ids.txt"), output);

            Console.WriteLine();
        }

        private static async Task<Dictionary<string, string>> DownloadChunk(string downloadFolder, bool cleanUp, List<string> updateIds)
        {
            // Get the URLs
            List<string> revisionIds = Enumerable.Repeat("1", updateIds.Count).ToList();
            string token = await Authentication.GetMicrosoftToken("msAuthInfo.json");

            IList<Uri> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds, $"<User>{token}</User>");

            HttpClient client = new HttpClient();

            Dictionary<string, string> validUpdates = new Dictionary<string, string>();

            // Download and check each update
            int i = 0;
            foreach (Uri uri in Files)
            {
                if (uri.Host == "test.com")
                {
                    i++;
                    continue;
                }

                // Get the header of the download link and see if we get a package name from there so we can skip downloading
                HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
                if (response.Content.Headers.ContentDisposition != null)
                {
                    if (response.Content.Headers.ContentDisposition.FileName.StartsWith("Microsoft.Minecraft"))
                    {
                        Console.WriteLine($"{updateIds[i]} is Minecraft");

                        if (!cleanUp)
                        {
                            Console.WriteLine($"Downloading {updateIds[i]}");
                            await client.DownloadFileTaskAsync(uri, Path.Join(downloadFolder, response.Content.Headers.ContentDisposition.FileName), (s, e) => Console.Write("\r{0}%", e.ProgressPercentage));

                            Console.WriteLine();
                        }

                        validUpdates.Add(updateIds[i], response.Content.Headers.ContentDisposition.FileName);
                        i++;
                        continue;
                    }
                    else
                    {
                        Console.WriteLine($"{updateIds[i]} is another package {response.Content.Headers.ContentDisposition.FileName}");
                        i++;
                        continue;
                    }
                }

                string downloadLocation = Path.Join(downloadFolder, updateIds[i]);

                bool valid = false;
                string outName = "";
                try
                {
                    Console.WriteLine($"Downloading {updateIds[i]}");
                    await client.DownloadFileTaskAsync(uri, downloadLocation, (s, e) => Console.Write("\r{0}%", e.ProgressPercentage));

                    Console.WriteLine();

                    Console.WriteLine($"Checking");

                    // Read the downloaded file as a zip
                    // This will only work with appx files so we can validate the download
                    using (ZipArchive zip = ZipFile.Open(downloadLocation, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            if (entry.Name == "AppxManifest.xml")
                            {
                                // Load the AppxManifest to work out the filename
                                XmlDocument doc = new XmlDocument();
                                doc.Load(entry.Open());

                                XmlNode identity = doc.GetElementsByTagName("Identity")[0];
                                string publisher = identity.Attributes["Publisher"].Value.StartsWith("CN=Microsoft Corporation") ? "__8wekyb3d8bbwe" : "";
                                outName = $"{identity.Attributes["Name"].Value}_{identity.Attributes["Version"].Value}_{identity.Attributes["ProcessorArchitecture"].Value}{publisher}.Appx";

                                validUpdates.Add(updateIds[i], outName);
                                valid = true;
                            }
                            else if (entry.Name == "AppxBundleManifest.xml")
                            {
                                // Load the AppxBundleManifest to work out the filename
                                XmlDocument doc = new XmlDocument();
                                doc.Load(entry.Open());

                                XmlNode identity = doc.GetElementsByTagName("Identity")[0];
                                string publisher = identity.Attributes["Publisher"].Value.StartsWith("CN=Microsoft Corporation") ? "__8wekyb3d8bbwe" : "";
                                outName = $"{identity.Attributes["Name"].Value}_{identity.Attributes["Version"].Value}{publisher}.AppxBundle";

                                validUpdates.Add(updateIds[i], outName);
                                valid = true;
                            }
                        }
                    }

                    Console.WriteLine(valid ? "Valid" : "Invalid");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid");
                    Console.WriteLine(ex.Message);
                    File.Delete(downloadLocation);
                }

                // Cleanup the file if needed or rename
                if (cleanUp && valid)
                {
                    File.Delete(downloadLocation);
                }
                else if (valid)
                {
                    File.Move(downloadLocation, Path.Join(downloadFolder, outName), true);
                }

                i++;
            }

            return validUpdates;
        }
    }
}
