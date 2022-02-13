using Microsoft.Isam.Esent.Interop;
using StoreLib.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace DataStoreExtractor
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            string[] wantedIds = new string[] {
                "9NBLGGH2JHXJ", // Microsoft.MinecraftUWP
	            "9P5X4QVLC2XR" // Microsoft.MinecraftWindowsBeta
            };

            Dictionary<Guid, string> updatePackageMap = new Dictionary<Guid, string>();

            // Setup the edb reading
            #region Load DB
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
            #endregion

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

            // Ask the user what they want to do
            Console.WriteLine();
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

            // Get the URLs
            List<string> updateIds = updatePackageMap.Keys.Select(x => x.ToString()).ToList();
            List<string> revisionIds = Enumerable.Repeat("1", updateIds.Count).ToList();

            IList<Uri> Files = await FE3Handler.GetFileUrlsAsync(updateIds, revisionIds, $"<User>{Authentication.GetWUToken()}</User>"); // TODO: Maybe add auth here?

            WebClient wc = new WebClient();
            wc.DownloadProgressChanged += (s, e) => Console.Write("\r{0}%", e.ProgressPercentage);

            Dictionary<string, string> validUpdates = new Dictionary<string, string>();
            string downloadFolder = "./Downloads/";
            Directory.CreateDirectory(downloadFolder);

            // Download and check each update
            int i = 0;
            foreach (Uri uri in Files)
            {
                if (uri.Host == "test.com")
                {
                    i++;
                    continue;
                }

                string downloadLocation = Path.Join(downloadFolder, updateIds[i]);

                Console.WriteLine($"Downloading {updateIds[i]}");
                await wc.DownloadFileTaskAsync(uri, downloadLocation);

                Console.WriteLine();

                Console.WriteLine($"Checking");
                bool valid = false;
                string outName = "";
                try
                {
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
                                outName = $"{identity.Attributes["Name"].Value}_{identity.Attributes["Version"].Value}_{identity.Attributes["ProcessorArchitecture"].Value}__8wekyb3d8bbwe.Appx";

                                validUpdates.Add(updateIds[i], outName);
                                valid = true;
                            }
                        }
                    }

                    Console.WriteLine("Valid");
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
        }
    }
}
