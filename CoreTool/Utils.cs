using System;
using System.Net;

namespace CoreTool
{
    internal class Utils
    {

        public static string GetVersionFromName(string name)
        {
            string rawVer = name.Split("_")[1];
            string[] verParts = rawVer.Split('.');

            // Check if we are a pre-v1 version as they have a different format
            if (verParts[0] == "0")
            {
                string lastBit = verParts[1].Substring(2).TrimStart('0');
                string firstBit = verParts[1].Substring(0, 2);

                if (lastBit == "")
                {
                    lastBit = "0";
                }

                return $"{verParts[0]}.{firstBit}.{lastBit}.{verParts[2]}";
            }
            else
            {
                verParts[2] = verParts[2].PadLeft(2, '0');
                string lastBit = verParts[2].Substring(verParts[2].Length - 2).TrimStart('0');
                string firstBit = verParts[2].Substring(0, verParts[2].Length - 2);

                if (firstBit == "")
                {
                    firstBit = "0";
                }

                if (lastBit == "")
                {
                    lastBit = "0";
                }

                return $"{verParts[0]}.{verParts[1]}.{firstBit}.{lastBit}";
            }
        }

        public static string GetArchFromName(string name)
        {
            return name.Split("_")[2];
        }

        /// <summary>
        /// Update the current line with the download progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write("\r{0}%", e.ProgressPercentage);
        }
    }
}