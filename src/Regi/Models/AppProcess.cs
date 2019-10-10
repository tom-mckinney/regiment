﻿using McMaster.Extensions.CommandLineUtils;
using Regi.Extensions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Regi.Models
{
    public class AppProcess
    {
        private object _lock = new object();

        public AppProcess(Process process, AppTask task, AppStatus status, int? port = null)
        {
            Process = process;
            Task = task;
            Status = status;
            Port = port;
        }

        public AppTask Task { get; set; }

        public AppStatus Status { get; set; }

        public virtual Process Process { get; protected set; }

        public int ProcessId { get; private set; }
        public string ProcessName { get; private set; }

        public int? Port { get; set; }

        public string Path { get; set; }

        public DateTimeOffset? StartTime
        {
            get
            {
                try
                {
                    return Process?.StartTime;
                }
                catch
                {
                    return null;
                }
            }
        }

        public DateTimeOffset? EndTime { get; set; }

        public bool KillOnExit { get; set; } = true;

        public bool Verbose { get; set; } = false;

        public bool RawOutput { get; internal set; }

        public bool ErrorDataHandled { get; internal set; } = false;
        public bool OutputDataHandled { get; internal set; } = false;


        public void Start()
        {
            if (Process == null)
                throw new InvalidOperationException("Process cannot be null when starting");

            lock (_lock)
            {
                Process.Start();

                if (!RawOutput)
                {
                    Process.BeginErrorReadLine();
                    Process.BeginOutputReadLine();
                }

                ProcessId = Process.Id;
                ProcessName = Process.ProcessName;
            }
        }

        public Action<int> OnKill { get; set; }

        public void WaitForExit()
        {
            Process?.WaitForExit();
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken)
        {
            if (Process == null)
                return System.Threading.Tasks.Task.CompletedTask;

            return Process.WaitForExitAsync(cancellationToken);
        }

        public void Kill(IConsole console = null)
        {
            Kill(Constants.DefaultTimeout, console);
        }

        public void Kill(TimeSpan timeout, IConsole console = null)
        {
            OnKill?.Invoke(ProcessId);

            if (KillOnExit)
            {
                try
                {
                    Process?.Kill(true);
                    Process?.WaitForExit(timeout.Milliseconds);
                }
                catch (Exception e)
                {
                    console?.WriteErrorLine($"Exception was thrown while exiting process with PID {ProcessId}. Details: {e.Message}");
                }
            }
        }
    }
}
