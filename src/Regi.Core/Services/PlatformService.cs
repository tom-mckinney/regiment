﻿using McMaster.Extensions.CommandLineUtils;
using Regi.Abstractions;
using Regi.Extensions;
using Regi.Models;
using Regi.Utilities;
using System.Diagnostics;

namespace Regi.Services
{
    public interface IPlatformService
    {
        IRuntimeInfo RuntimeInfo { get; }
        Process GetKillProcess(string processName, CommandOptions options);
        void RunAnonymousScript(string script, CommandOptions options, string workingDirectory = null);
    }

    public class PlatformService : IPlatformService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;

        public PlatformService(IRuntimeInfo runtimeInfo, IFileSystem fileSystem, IConsole console)
        {
            RuntimeInfo = runtimeInfo;
            _fileSystem = fileSystem;
            _console = console;
        }

        public IRuntimeInfo RuntimeInfo { get; private set; }

        public Process GetKillProcess(string processName, CommandOptions options)
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (RuntimeInfo.IsWindows)
            {
                startInfo.FileName = "taskkill";
                startInfo.Arguments = $"/F /IM {ProcessUtility.AddExtension(processName)}";
            }
            else
            {
                startInfo.FileName = "killall";
                startInfo.Arguments = processName;
            }

            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            process.OutputDataReceived += (o, e) =>
            {
                if (options.Verbose && !string.IsNullOrWhiteSpace(e.Data))
                {
                    _console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (o, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _console.WriteWarningLine(e.Data);
                }
            };

            return process;
        }

        public void RunAnonymousScript(string script, CommandOptions options, string workingDirectory = null)
        {
            string scriptExecutable = PathUtility.GetFileNameFromCommand(script);
            if (!PathUtility.TryGetPathFile(scriptExecutable, RuntimeInfo, out string fileName))
            {
                fileName = RuntimeInfo.IsWindows ? "powershell.exe" : "bash";
            }
            else
            {
                script = script.Remove(0, scriptExecutable.Length);
            }

            if (options.Verbose)
            {
                _console.WriteDefaultLine($"Executing '{fileName} {script}'");
            }

            using (var process = ProcessUtility.CreateProcess(fileName, script, _fileSystem, workingDirectory))
            {
                process.ErrorDataReceived += ProcessUtility.WriteOutput(_console, ConsoleLogLevel.Error);
                if (options.Verbose)
                {
                    process.OutputDataReceived += ProcessUtility.WriteOutput(_console);
                }

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit(5000);
            }
        }
    }
}
