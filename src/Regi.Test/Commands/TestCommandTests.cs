﻿using Moq;
using Regi.Commands;
using Regi.Models;
using Regi.Services;
using Regi.Test.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Commands
{
    public class TestCommandTests
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly TestConsole _console;
        private readonly Mock<IRunnerService> _runnerServiceMock = new Mock<IRunnerService>();
        private readonly IProjectManager _projectManager;
        private readonly Mock<IConfigurationService> _configServiceMock = new Mock<IConfigurationService>();
        private readonly ISummaryService _summaryService;

        public TestCommandTests(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            _console = new TestConsole(testOutput);
            _projectManager = new ProjectManager(_console, new Mock<ICleanupService>().Object);
            _summaryService = new SummaryService(_projectManager, _console);
        }

        TestCommand CreateCommand()
        {
            return new TestCommand(_runnerServiceMock.Object, _summaryService, _projectManager, _configServiceMock.Object, _console)
            {
                Name = null
            };
        }

        [Fact]
        public void Will_run_all_test_if_no_name_or_type_is_specified()
        {
            _configServiceMock.Setup(m => m.GetConfiguration())
                .Returns(SampleProjects.ConfigurationDefault)
                .Verifiable();
            _runnerServiceMock.Setup(m => m.Test(It.IsAny<IList<Project>>(), It.IsAny<RegiOptions>()))
                .Callback<IList<Project>, RegiOptions>((projects, options) =>
                {
                    foreach (var p in projects)
                    {
                        p.Processes.Add(new AppProcess(new Process(), AppTask.Test, AppStatus.Success));
                    }
                })
                .Returns<IList<Project>, RegiOptions>((projects, options) => projects)
                .Verifiable();

            TestCommand command = CreateCommand();

            int testProjectCount = command.OnExecute();

            Assert.Equal(0, testProjectCount);

            _configServiceMock.Verify();
            _runnerServiceMock.Verify();
        }

        [Fact]
        public void Returns_fail_count_as_exit_code()
        {
            _configServiceMock.Setup(m => m.GetConfiguration())
                .Returns(SampleProjects.ConfigurationDefault)
                .Verifiable();
            _runnerServiceMock.Setup(m => m.Test(It.IsAny<IList<Project>>(), It.IsAny<RegiOptions>()))
                .Callback<IList<Project>, RegiOptions>((projects, options) =>
                {
                    projects[0].Processes.Add(new AppProcess(new Process(), AppTask.Test, AppStatus.Failure));
                })
                .Returns<IList<Project>, RegiOptions>((projects, options) => projects)
                .Verifiable();

            TestCommand command = CreateCommand();

            int testProjectCount = command.OnExecute();

            Assert.Equal(1, testProjectCount);

            _configServiceMock.Verify();
            _runnerServiceMock.Verify();
        }

        [Theory]
        [InlineData(ProjectType.Unit)]
        [InlineData(ProjectType.Integration)]
        public void Will_only_run_tests_with_matching_type_if_specified(ProjectType? type)
        {
            _configServiceMock.Setup(m => m.GetConfiguration())
                .Returns(SampleProjects.ConfigurationDefault)
                .Verifiable();
            _runnerServiceMock.Setup(m => m.Test(It.Is<IList<Project>>(projects => projects.All(p => p.Type == type)), It.Is<RegiOptions>(o => o.Type == type)))
                .Callback<IList<Project>, RegiOptions>((projects, options) =>
                {
                    foreach (var p in projects)
                    {
                        p.Processes.Add(new AppProcess(new Process(), AppTask.Test, AppStatus.Success));
                    }
                })
                .Returns<IList<Project>, RegiOptions>((projects, options) => projects)
                .Verifiable();

            TestCommand command = CreateCommand();

            command.Type = type;

            int testProjectCount = command.OnExecute();

            Assert.Equal(0, testProjectCount);

            _configServiceMock.Verify();
            _runnerServiceMock.Verify();
        }
    }
}
