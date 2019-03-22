﻿using Regi.Models;
using Regi.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Regi.Commands
{
    public class InitalizeCommand : CommandBase
    {
        private readonly IRunnerService _runnerService;

        public InitalizeCommand(IRunnerService runnerService)
        {
            _runnerService = runnerService;
        }

        public override int OnExecute()
        {
            _runnerService.Initialize(Options);

            return 0;
        }
    }
}