﻿using McMaster.Extensions.CommandLineUtils;
using Regi.Models;
using Regi.Services;

namespace Regi.Commands
{
    [Command("test", AllowArgumentSeparator = true)]
    public class TestCommand : CommandBase
    {
        private readonly IRunnerService _runnerService;
        private readonly IConsole _console;

        public TestCommand(IRunnerService runnerService, IConsole console)
            : base(console)
        {
            _runnerService = runnerService;
            _console = console;
        }

        public override int OnExecute()
        {
            Projects = _runnerService.Test(Options);

            int projectCount = Projects.Count;

            foreach (var p in Projects)
            {
                p.Process.Dispose();
            }

            return Projects.Count;
        }
    }
}
