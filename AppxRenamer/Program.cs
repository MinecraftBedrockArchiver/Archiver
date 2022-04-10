using AuthenticodeExaminer;
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace AppxRenamer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Enter dir to check: ");

            string baseDir = Console.ReadLine();

            string[] files = Directory.GetFiles(baseDir, "*.appx", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                string outName = "";

                // Read the downloaded file as a zip
                // This will only work with appx files so we can validate the download
                using (ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        if (entry.Name == "AppxManifest.xml")
                        {
                            // Load the AppxManifest to work out the filename
                            XmlDocument doc = new XmlDocument();
                            doc.Load(entry.Open());

                            XmlNode identity = doc.GetElementsByTagName("Identity")[0];
                            outName = $"{identity.Attributes["Name"].Value}_{identity.Attributes["Version"].Value}_{identity.Attributes["ProcessorArchitecture"].Value}__8wekyb3d8bbwe";
                        }
                    }
                }

                FileInspector inspector = new FileInspector(file);
                SignatureCheckResult validationResult = inspector.Validate();

                if (validationResult == SignatureCheckResult.Valid)
                {
                    Console.WriteLine("Valid sig!");
                    bool foundMS = false;
                    foreach (AuthenticodeSignature signature in inspector.GetSignatures())
                    {
                        if (signature.PublisherInformation.Description == "Microsoft")
                        {
                            foundMS = true;
                            break;
                        }
                    }

                    if (!foundMS)
                    {
                        outName += "_signed_noms";
                    }
                }
                else if (validationResult == SignatureCheckResult.NoSignature)
                {
                    Console.WriteLine("No Sig!");
                    outName += "_unsigned";
                }
                else
                {
                    Console.WriteLine("Bad Sig!");
                    outName += "_badsignature";
                }

                outName += ".Appx";

                Console.WriteLine($"Moving {file} to {outName}");
                File.Move(file, Path.Join(baseDir, outName), true);
            }
        }
    }
}
