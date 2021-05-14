﻿using Regi.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Regi.Runtime
{
    public class ProcessManager : IProcessManager
    {
        private readonly ILogSinkManager _logSinkManager;
        private readonly IFileSystem _fileSystem;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly ConcurrentDictionary<Guid, IManagedProcess> _managedProcesses = new(); 

        public ProcessManager(ILogSinkManager logSinkManager, IFileSystem fileSystem, IRuntimeInfo runtimeInfo)
        {
            _logSinkManager = logSinkManager;
            _fileSystem = fileSystem;
            _runtimeInfo = runtimeInfo;
        }

        internal IReadOnlyDictionary<Guid, IManagedProcess> ManagedProcesses => _managedProcesses;

        public async ValueTask<IManagedProcess> CreateAsync(string serviceName, string fileName, string arguments, DirectoryInfo workingDirectory = null)
        {
            if (workingDirectory == null)
            {
                workingDirectory = new DirectoryInfo(_fileSystem.WorkingDirectory);
            }

            var id = Guid.NewGuid();
            var logSink = await _logSinkManager.CreateAsync(serviceName, id);
            var managedProcess = new ManagedProcess(id, fileName, arguments, workingDirectory, logSink, _runtimeInfo);

            if (!_managedProcesses.TryAdd(managedProcess.Id, managedProcess))
            {
                throw new InvalidOperationException($"Managed process with Id of {managedProcess.Id} already exists");
            }

            return managedProcess;
        }

        public async ValueTask<bool> ShutdownAsync(Guid managedProcessId, CancellationToken cancellationToken)
        {
            if (!_managedProcesses.TryRemove(managedProcessId, out IManagedProcess managedProcess))
            {
                throw new InvalidOperationException($"No managed process with Id of {managedProcessId}");
            }

            await managedProcess.KillAsync(cancellationToken);

            return true;
        }
    }
}