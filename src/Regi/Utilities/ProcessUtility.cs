﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Regi.Utilities
{
    public static class ProcessUtility
    {
        public static string AddExtension(string name, string extension = ".exe")
        {
            if (name.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
            {
                return name;
            }

            return name + extension;
        }

        public static void GetAllChildIdsUnix(int parentId, ISet<int> children)
        {
            var exitCode = RunProcessAndWaitForExit(
                "pgrep",
                $"-P {parentId}",
                out string stdout,
                out string _);

            if (exitCode == 0 && !string.IsNullOrEmpty(stdout))
            {
                using (var reader = new StringReader(stdout))
                {
                    while (true)
                    {
                        var text = reader.ReadLine();
                        if (text == null)
                        {
                            return;
                        }

                        int id;
                        if (int.TryParse(text, out id))
                        {
                            children.Add(id);
                            // Recursively get the children
                            GetAllChildIdsUnix(id, children);
                        }
                    }
                }
            }
        }

        public static void KillProcessUnix(int processId, out string stdout, out string stderr)
        {
            RunProcessAndWaitForExit(
                "kill",
                $"-TERM {processId}",
                out stdout, out stderr);
        }

        public static int RunProcessAndWaitForExit(string fileName, string arguments, out string stdout, out string stderr)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);

            stdout = null;
            stderr = null;
            if (process.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
            {
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
            }
            else
            {
                process.Kill();

                // Kill is asynchronous so we should still wait a little
                //
                process.WaitForExit((int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            return process.HasExited ? process.ExitCode : -1;
        }
    }
}
