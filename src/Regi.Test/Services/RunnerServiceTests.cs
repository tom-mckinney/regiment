﻿using Moq;
using Newtonsoft.Json;
using Regi.Extensions;
using Regi.Models;
using Regi.Services;
using Regi.Test.Helpers;
using Regi.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Services
{
    public class RunnerServiceTests
    {
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console;
        private readonly Mock<IDotnetService> _dotnetServiceMock;
        private readonly Mock<INodeService> _nodeServiceMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly IRunnerService _runnerService;

        private readonly string _startupConfigGood;
        private readonly string _startupConfigBad;
        private readonly char Slash = Path.DirectorySeparatorChar;

        private const int dotnetAppCount = 2;
        private const int nodeAppCount = 1;
        private const int totalAppCount = dotnetAppCount + nodeAppCount;

        private const int dotnetTestCount = 2;
        private const int nodeTestCount = 0;
        private const int totalTestCount = dotnetTestCount + nodeTestCount;

        public RunnerServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _console = new TestConsole(output);
            _dotnetServiceMock = new Mock<IDotnetService>();
            _nodeServiceMock = new Mock<INodeService>();
            _fileServiceMock = new Mock<IFileService>();
            _runnerService = new RunnerService(
                _dotnetServiceMock.Object,
                _nodeServiceMock.Object,
                new TestParallelService(),
                _fileServiceMock.Object,
                _console);

            _startupConfigGood = SampleDirectoryPath("ConfigurationGood");
            _startupConfigBad = SampleDirectoryPath("ConfigurationBad");
        }

        [Fact]
        public void GetStatupConfig_throws_exception_when_directory_not_found()
        {
            DirectoryUtility.SetTargetDirectory(SampleDirectoryPath("BUNK_DIRECTORY"));
            Assert.Throws<DirectoryNotFoundException>(() => _runnerService.GetStartupConfig());
        }

        [Fact]
        public void GetStatupConfig_throws_exception_when_startup_config_not_found()
        {
            DirectoryUtility.SetTargetDirectory(SampleDirectoryPath("SampleAppError"));
            Assert.Throws<FileNotFoundException>(() => _runnerService.GetStartupConfig());
        }

        [Theory]
        [InlineData("ConfigurationBad")]
        [InlineData("ConfigurationWrongEnum")]
        public void GetStatupConfig_throws_exception_when_startup_config_has_bad_format(string configuration)
        {
            DirectoryUtility.SetTargetDirectory(SampleDirectoryPath(configuration));
            var ex = Assert.Throws<JsonSerializationException>(() => _runnerService.GetStartupConfig());

            _console.WriteErrorLine(nameof(ProjectType));
            ex.LogAndReturnStatus(_console);
        }

        [Fact]
        public void GetStartupConfig_returns_configuration_model_when_run_in_directory_with_startup_file()
        {
            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            StartupConfig startupConfig = _runnerService.GetStartupConfig();

            Assert.Equal(totalAppCount, startupConfig.Apps.Count);
            Assert.Equal(totalTestCount, startupConfig.Tests.Count);
            Assert.Empty(startupConfig.Services);
        }

        [Fact]
        public void Start_returns_a_process_for_every_app_in_startup_config()
        {
            _dotnetServiceMock.Setup(m => m.RunProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns<Project, bool, int?>((p, b, i) => new AppProcess(new Process(), AppTask.Start, AppStatus.Success, i))
                .Verifiable();
            _nodeServiceMock.Setup(m => m.StartProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns<Project, bool, int?>((p, b, i) => new AppProcess(new Process(), AppTask.Start, AppStatus.Success, i))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var processes = _runnerService.Start(new CommandOptions());

            Assert.Equal(totalAppCount, processes.Count);
            Assert.Single(processes, p => p.Port == 9080);

            _dotnetServiceMock.Verify(m => m.RunProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()), Times.Exactly(dotnetAppCount));
            _nodeServiceMock.Verify(m => m.StartProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()), Times.Exactly(nodeAppCount));
        }

        [Fact]
        public void Start_sets_url_for_every_app()
        {
            _dotnetServiceMock.Setup(m => m.RunProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns<Project, bool, int?>((p, b, i) => new AppProcess(new Process(), AppTask.Start, AppStatus.Success, i))
                .Verifiable();
            _nodeServiceMock.Setup(m => m.StartProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns<Project, bool, int?>((p, b, i) => new AppProcess(new Process(), AppTask.Start, AppStatus.Success, i))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var projects = _runnerService.Start(new CommandOptions());

            foreach (var p in projects)
            {
                if (p.Port.HasValue)
                {
                    string urlName = $"{p.Name}_PORT".ToUnderscoreCase();
                    string projectUrl = Environment.GetEnvironmentVariable(urlName);
                    Assert.True(projectUrl != null, $"URL has not been stored as environment variable: {urlName}");
                    Assert.Equal($"http://localhost:{p.Port}", projectUrl);
                }
            }
        }

        [Fact]
        public void Test_returns_a_process_for_every_test_in_startup_config()
        {
            _dotnetServiceMock.Setup(m => m.TestProject(It.IsAny<Project>(), It.IsAny<bool>()))
                .Returns(new AppProcess(new Process(), AppTask.Test, AppStatus.Success))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var processes = _runnerService.Test(new CommandOptions());

            Assert.Equal(totalTestCount, processes.Count);

            _dotnetServiceMock.VerifyAll();
        }

        [Fact]
        public void Test_also_starts_every_requirement_for_each_test()
        {
            _dotnetServiceMock.Setup(m => m.TestProject(It.IsAny<Project>(), It.IsAny<bool>()))
                .Returns(new AppProcess(new Process(), AppTask.Test, AppStatus.Success))
                .Verifiable();
            _dotnetServiceMock.Setup(m => m.RunProject(It.IsAny<Project>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new AppProcess(new Process(), AppTask.Start, AppStatus.Success))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(SampleDirectoryPath("ConfigurationRequires"));

            var processes = _runnerService.Test(new CommandOptions());

            Assert.Equal(totalTestCount + 1, processes.Count);
            Assert.Single(processes, p => p.Task == AppTask.Start);
            Assert.Equal(2, processes.Where(p => p.Task == AppTask.Test).Count());

            _dotnetServiceMock.VerifyAll();
        }

        [Theory]
        [InlineData(ProjectType.Unit, 1)]
        [InlineData(ProjectType.Integration, 1)]
        public void Test_will_only_run_tests_on_type_specified(ProjectType type, int expectedCount)
        {
            _dotnetServiceMock.Setup(m => m.TestProject(It.IsAny<Project>(), It.IsAny<bool>()))
                .Returns(new AppProcess(new Process(), AppTask.Test, AppStatus.Success))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var processes = _runnerService.Test(new CommandOptions { Type = type });

            Assert.Equal(expectedCount, processes.Count);

            _dotnetServiceMock.VerifyAll();
        }

        [Fact]
        public void Install_returns_a_process_for_every_app_and_test_project()
        {
            _dotnetServiceMock.Setup(m => m.RestoreProject(It.IsAny<Project>(), It.IsAny<bool>()))
                .Returns(new AppProcess(new Process(), AppTask.Install, AppStatus.Success))
                .Verifiable();
            _nodeServiceMock.Setup(m => m.InstallProject(It.IsAny<Project>(), It.IsAny<bool>()))
                .Returns(new AppProcess(new Process(), AppTask.Install, AppStatus.Success))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var processes = _runnerService.Install(new CommandOptions());

            Assert.Equal(totalAppCount + totalTestCount, processes.Count);

            _dotnetServiceMock.VerifyAll();
        }

        [Fact]
        public void Initialize_returns_a_single_process_and_creates_a_config_file()
        {
            _fileServiceMock.Setup(m => m.CreateConfigFile())
                .Returns(new FileInfo("regi.json"))
                .Verifiable();

            DirectoryUtility.SetTargetDirectory(SampleDirectoryPath("ConfigurationNew"));

            _runnerService.Initialize(new CommandOptions());

            _fileServiceMock.VerifyAll();
        }

        [Fact]
        public void List_prints_all_apps_and_tests()
        {
            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var output = _runnerService.List(new CommandOptions());

            Assert.Equal(totalAppCount, output.Apps.Count);
            Assert.Equal(totalTestCount, output.Tests.Count);

            Assert.Contains("Apps:", _console.LogOutput);
            Assert.Contains("Tests:", _console.LogOutput);
        }

        [Theory]
        [InlineData("node", 1, 0)]
        [InlineData("test", 0, 2)]
        [InlineData("SampleApp1", 1, 0)]
        public void List_prints_only_apps_or_tests_that_match_name_if_specified(string name, int appCount, int testCount)
        {
            DirectoryUtility.SetTargetDirectory(_startupConfigGood);

            var output = _runnerService.List(new CommandOptions { Name = name });

            Assert.Equal(appCount, output.Apps.Count);
            Assert.Equal(testCount, output.Tests.Count);

            if (appCount <= 0)
                Assert.DoesNotContain("Apps:", _console.LogOutput);
            if (testCount <= 0)
                Assert.DoesNotContain("Tests:", _console.LogOutput);
        }

        internal string SampleDirectoryPath(string name)
        {
            string path = $"{Directory.GetCurrentDirectory()}{Slash}_SampleProjects_{Slash}{name}";

            return path;
        }
    }
}
