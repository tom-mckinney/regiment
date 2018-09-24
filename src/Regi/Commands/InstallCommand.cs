﻿using McMaster.Extensions.CommandLineUtils;
using Regi.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Regi.Commands
{
    public class InstallCommand : CommandBase
    {
        private readonly IRunnerService _runnerService;
        private readonly IConsole _console;

        public InstallCommand(IRunnerService runnerService, IConsole console)
        {
            _runnerService = runnerService;
            _console = console;
        }

        public override int OnExecute()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            var unitTests = _runnerService.Install(currentDirectory);

            return unitTests.Count;
        }
    }
}