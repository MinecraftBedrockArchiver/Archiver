using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

namespace CoreTool
{
    internal class Utils
    {
        public static readonly Task CompletedTask = Task.FromResult(false);

        public static readonly Log GenericLogger = new Log("Util");

        public static string GetVersionFromName(string name)
        {
            string fileName = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name).ToLower();
            if (extension == ".appx")
            {
                return GetVersionFromNameAppx(fileName);
            }
            else if (extension == ".appxbundle")
            {
                string version = fileName.Split("_")[1];
                if (version.Split(".").Length > 3)
                {
                    version = Regex.Replace(version, "\\.00?$", "");
                }

                return version;
            }
            else
            {
                return "unknown";
            }
        }

        private static string GetVersionFromNameAppx(string name)
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
            string fileName = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name).ToLower();
            if (extension.StartsWith(".appx"))
            {
                return fileName.Split("_")[2];
            }
            else
            {
                return "unknown";
            }
        }

        // https://stackoverflow.com/a/10789196/5299903
        public static Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments = "", string workingDir = "./")
        {
            var tcs = new TaskCompletionSource<(int ExitCode, string Output)>();
            string output = "";

            var process = new Process
            {
                StartInfo = {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (sender, args) => output += args.Data;

            process.Exited += (sender, args) =>
            {
                tcs.SetResult((ExitCode: process.ExitCode, Output: output));
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();

            return tcs.Task;
        }

        /// <summary>
        /// Re-implementation of the native StrCmpLogicalW from https://stackoverflow.com/a/5641272/5299903
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int StrCmpLogicalW(string x, string y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            int lx = x.Length, ly = y.Length;

            for (int mx = 0, my = 0; mx < lx && my < ly; mx++, my++)
            {
                if (char.IsDigit(x[mx]) && char.IsDigit(y[my]))
                {
                    long vx = 0, vy = 0;

                    for (; mx < lx && char.IsDigit(x[mx]); mx++)
                        vx = vx * 10 + x[mx] - '0';

                    for (; my < ly && char.IsDigit(y[my]); my++)
                        vy = vy * 10 + y[my] - '0';

                    if (vx != vy)
                        return vx > vy ? 1 : -1;
                }

                if (mx < lx && my < ly && x[mx] != y[my])
                    return x[mx] > y[my] ? 1 : -1;
            }

            return lx - ly;
        }
    }
}