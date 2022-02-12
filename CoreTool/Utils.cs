﻿using System.Diagnostics;
using System.Threading.Tasks;

namespace CoreTool
{
    internal class Utils
    {
        public static readonly Task CompletedTask = Task.FromResult(false);

        public static readonly Log GenericLogger = new Log();

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
    }
}