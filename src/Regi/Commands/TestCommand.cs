﻿using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Abstractions;
using Regi.Models;
using Regi.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Regi.Commands
{
    [Command("unit")]
    public class TestCommand : CommandBase
    {
        private readonly IRunnerService _runnerService;
        private readonly IConsole _console;

        public TestCommand(IRunnerService runnerService, IConsole console)
        {
            _runnerService = runnerService;
            _console = console;
        }

        [Option(Description = "Type of tests to run", ShortName = "t", LongName = "type")]
        public ProjectType? Type { get; set; }

        public override int OnExecute()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

             var unitTests = _runnerService.Test(currentDirectory, Type);

            return unitTests.Count;
        }
    }
}
