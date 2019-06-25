﻿using McMaster.Extensions.CommandLineUtils;
using Regi.Constants;
using Regi.Extensions;
using Regi.Models;
using Regi.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Regi.Services
{
    public interface IFrameworkService
    {
        AppProcess InstallProject(Project project, RegiOptions options);
        AppProcess StartProject(Project project, RegiOptions options);
        AppProcess TestProject(Project project, RegiOptions options);
        AppProcess BuildProject(Project project, RegiOptions options);
        AppProcess KillProcesses(RegiOptions options);
    }

    public abstract class FrameworkService : IFrameworkService
    {
        protected readonly IConsole _console;
        protected readonly IPlatformService _platformService;
        protected readonly string _frameworkExePath;
        protected readonly object _lock = new object();

        public FrameworkService(IConsole console, IPlatformService platformService, string frameworkExePath)
        {
            _console = console;
            _platformService = platformService;

            if (string.IsNullOrWhiteSpace(frameworkExePath))
            {
                throw new ArgumentException("Cannot have a null or empty framework executable path.", nameof(frameworkExePath));
            }

            _frameworkExePath = frameworkExePath;
        }

        public abstract AppProcess InstallProject(Project project, RegiOptions options);
        public abstract AppProcess StartProject(Project project, RegiOptions options);
        public abstract AppProcess TestProject(Project project, RegiOptions options);
        public abstract AppProcess BuildProject(Project project, RegiOptions options);

        protected virtual void SetEnvironmentVariables(Process process, Project project)
        {
            process.StartInfo.EnvironmentVariables.TryAdd("END_TO_END_TESTING", bool.TrueString);
            process.StartInfo.EnvironmentVariables.TryAdd("IN_MEMORY_DATABASE", bool.TrueString);
            process.StartInfo.EnvironmentVariables.TryAdd("DONT_STUB_PLAID", bool.FalseString);
        }

        protected abstract ProjectOptions FrameworkOptions { get; }

        protected virtual IList<string> FrameworkWarningIndicators { get; } = new List<string>();

        protected virtual void ApplyFrameworkOptions(StringBuilder builder, string command, Project project, RegiOptions options)
        {
            lock (_lock)
            {
                if (FrameworkOptions != null && FrameworkOptions.Any())
                {
                    if (FrameworkOptions.TryGetValue(command, out IList<string> defaultOptions))
                    {
                        builder.Append(' ').AppendJoin(' ', defaultOptions);
                    }

                    if (FrameworkOptions.TryGetValue(FrameworkCommands.Any, out IList<string> anyCommandOptions))
                    {
                        builder.Append(' ').AppendJoin(' ', anyCommandOptions);
                    }
                }
            }
        }

        protected virtual string FormatAdditionalArguments(string[] args) => string.Join(' ', args);

        public virtual AppProcess CreateProcess(string command, RegiOptions options, string fileName = null)
        {
            fileName = fileName ?? _frameworkExePath;
            string args = command;

            if (options.Verbose)
                _console.WriteEmphasizedLine($"Executing: {fileName} {args}");

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = DirectoryUtility.TargetDirectoryPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            AppProcess output = new AppProcess(process, AppTask.Cleanup, AppStatus.Running)
            {
                Verbose = options.Verbose
            };

            process.Exited += HandleExited(output);

            if (options.Verbose)
            {
                process.OutputDataReceived += HandleDataReceivedAnonymously();
                output.OutputDataHandled = true;
            }
            else
            {
                process.OutputDataReceived += HandleDataReceivedSilently();
            }

            return output;
        }

        public virtual AppProcess CreateProcess(string command, Project project, RegiOptions options, string fileName = null)
        {
            fileName = fileName ?? _frameworkExePath;
            string args = BuildCommand(command, project, options);

            if (options.Verbose)
                _console.WriteEmphasizedLine($"Executing: {fileName} {args}");

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = project.File.DirectoryName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            AppProcess output = new AppProcess(process, FrameworkCommands.GetAppTask(command), AppStatus.Running, project.Port)
            {
                KillOnExit = options.KillProcessesOnExit,
                Verbose = options.Verbose,
                OnDispose = (processId) => HandleDispose(project, processId, options)
            };

            process.StartInfo.CopyEnvironmentVariables(options.VariableList);
            SetEnvironmentVariables(process, project);

            process.Exited += HandleExited(output);

            if (project.RawOutput || options.RawOutput)
            {
                output.RawOutput = true;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                return output;
            }
            else
            {
                process.ErrorDataReceived += HandleErrorDataReceived(project.Name, output);
                output.ErrorDataHandled = true;

                if (options.Verbose || options.ShowOutput?.Any(x => x.Equals(project.Name, StringComparison.InvariantCultureIgnoreCase)) == true)
                {
                    process.OutputDataReceived += HandleOutputDataRecieved(project.Name);
                    output.OutputDataHandled = true;
                }
                else
                {
                    process.OutputDataReceived += HandleDataReceivedSilently();
                }
            }

            return output;
        }

        public virtual string BuildCommand(string command, Project project, RegiOptions options)
        {
            lock (_lock)
            {
                if (project?.Commands != null && project.Commands.TryGetValue(command, out string customCommand))
                {
                    command = customCommand;
                }

                StringBuilder builder = new StringBuilder();

                builder.Append(command);

                if (project?.Options?.Count > 0)
                {
                    foreach (var commandOption in project.Options)
                    {
                        if (commandOption.Key == "*" || commandOption.Key == command)
                        {

                            builder.Append(' ').AppendJoin(' ', commandOption.Value);
                        }
                    }
                }

                ApplyFrameworkOptions(builder, command, project, options);

                if (options?.RemainingArguments?.Count() > 0)
                {
                    builder.Append(' ').Append(FormatAdditionalArguments(options.RemainingArguments));
                }

                return builder.ToString();
            }
        }

        public virtual DataReceivedEventHandler HandleOutputDataRecieved(string name) => new DataReceivedEventHandler((o, e) =>
        {
            _console.WriteDefaultLine(name + ": " + e.Data, ConsoleLineStyle.Normal);
        });

        public virtual DataReceivedEventHandler HandleErrorDataReceived(string name, AppProcess output) => new DataReceivedEventHandler((o, e) =>
        {
            lock (_lock)
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    if (FrameworkWarningIndicators.Any(i => e.Data.StartsWith(i, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        _console.WriteWarningLine(name + ": " + e.Data);
                    }
                    else
                    {
                        _console.WriteErrorLine(name + ": " + e.Data);
                    }
                }
            }
        });

        public virtual DataReceivedEventHandler HandleDataReceivedAnonymously() => new DataReceivedEventHandler((o, e) =>
        {
            _console.WriteDefaultLine(e.Data, ConsoleLineStyle.Normal);
        });

        public virtual DataReceivedEventHandler HandleDataReceivedSilently() => new DataReceivedEventHandler((o, e) =>
        {
        });

        public virtual EventHandler HandleExited(AppProcess output) => new EventHandler((o, e) =>
        {
            lock (_lock)
            {
                output.EndTime = DateTimeOffset.UtcNow;

                if (o is Process process)
                {
                    if (process.HasExited && process.ExitCode == 0)
                    {
                        output.Status = AppStatus.Success;
                    }
                    else
                    {
                        output.Status = AppStatus.Failure;
                    }
                }
                else
                {
                    throw new InvalidOperationException("HandleExited event handler must be assigned to a Process object.");
                }
            }
        });

        protected virtual void HandleDispose(Project project, int processId, RegiOptions options)
        {
            if (options.Verbose)
            {
                _console.WriteEmphasizedLine($"Disposing process for project {project.Name} ({processId})");
            }
        }

        public abstract AppProcess KillProcesses(RegiOptions options);
    }
}
