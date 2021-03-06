﻿using Moq;
using Regi.Abstractions;
using Regi.Frameworks;
using Regi.Models;
using Regi.Services;
using Regi.Test.Extensions;
using Regi.Test.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Frameworks
{
    [Collection(TestCollections.NoParallel)]
    public class DotnetTests : TestBase
    {
        private readonly TestConsole _console;
        private readonly IDotnet _service;
        private readonly Mock<IRuntimeInfo> _runtimeInfoMock = new Mock<IRuntimeInfo>();

        private readonly Project _successfulTests;
        private readonly Project _failedTests;
        private readonly Project _consoleApp;
        private readonly Project _consoleFailureApp;
        private readonly Project _webApp;

        public DotnetTests(ITestOutputHelper testOutput)
        {
            _console = new TestConsole(testOutput);
            _service = new Dotnet(_fileSystem, _console, new PlatformService(_runtimeInfoMock.Object, _fileSystem, _console));

            _successfulTests = SampleProjects.XunitTests;
            _failedTests = SampleProjects.XunitFailureTests;
            _consoleApp = SampleProjects.Console;
            _consoleFailureApp = SampleProjects.ConsoleFailure;
            _webApp = SampleProjects.Backend;
        }

        [Fact]
        public async Task TestProject_on_success_returns_status()
        {
            var unitTest = await _service.Test(_successfulTests, _successfulTests.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Success, unitTest.Status);
        }

        [Fact]
        public async Task TestProject_verbose_on_success_prints_all_output()
        {
            var unitTest = await _service.Test(_successfulTests, _successfulTests.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Success, unitTest.Status);
            Assert.NotEmpty(_console.LogOutput);
        }

        [Fact]
        public async Task TestProject_on_failure_prints_only_exception_info()
        {
            var options = TestOptions.Create();

            options.Verbose = false;

            var unitTest = await _service.Test(_failedTests, _failedTests.GetAppDirectoryPaths(_fileSystem)[0], options, CancellationToken.None);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Failure, unitTest.Status);

            Assert.Contains("SampleFailedTests.UnitTest1.Test1 [FAIL]", _console.LogOutput, StringComparison.InvariantCulture);
            Assert.DoesNotMatch("Total:\\s*\\d", _console.LogOutput);
        }

        [Fact]
        public async Task TestProject_verbose_on_failure_prints_all_output()
        {
            var unitTest = await _service.Test(_failedTests, _failedTests.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Failure, unitTest.Status);

            Assert.Contains("Failed!", _console.LogOutput, StringComparison.InvariantCulture);
            Assert.Matches("Failed:\\s*1", _console.LogOutput);
            Assert.Matches("Passed:\\s*0", _console.LogOutput);
            Assert.Matches("Total:\\s*1", _console.LogOutput);
        }

        [Fact]
        public async Task RunProject_changes_status_from_running_to_success_on_exit()
        {
            var options = TestOptions.Create();

            var process = await _service.Start(_consoleApp, _consoleApp.GetAppDirectoryPaths(_fileSystem)[0], options, CancellationToken.None);

            Assert.Equal(AppStatus.Running, process.Status);

            await process.WaitForExitAsync(CancellationToken.None);

            Assert.Equal(AppStatus.Success, process.Status);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_returns_failure_status_on_thrown_exception()
        {
            var process = await _service.Start(_consoleFailureApp, _consoleFailureApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            await process.WaitForExitAsync(CancellationToken.None);

            Assert.Equal(AppStatus.Failure, process.Status);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_without_verbose_starts_and_prints_nothing()
        {
            CommandOptions optionsWithoutVerbose = new CommandOptions { Verbose = false, KillProcessesOnExit = false };

            var process = await _service.Start(_consoleApp, _consoleApp.GetAppDirectoryPaths(_fileSystem)[0], optionsWithoutVerbose, CancellationToken.None);

            await process.WaitForExitAsync(CancellationToken.None);

            await Task.Delay(100); // flakes out for some reason

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Null(_console.LogOutput);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_verbose_starts_and_prints_all_output()
        {
            var process = await _service.Start(_consoleApp, _consoleApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            await process.WaitForExitAsync(CancellationToken.None);

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Contains("Hello World!", _console.LogOutput, StringComparison.InvariantCulture);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_web_starts_and_prints_nothing()
        {
            var process = await _service.Start(_webApp, _webApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Thread.Sleep(1000);

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Running, process.Status);
            Assert.Equal(_webApp.Port, process.Port);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_will_start_custom_port_if_specified()
        {
            _webApp.Port = 8080;

            var process = await _service.Start(_webApp, _webApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Thread.Sleep(1000);

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Running, process.Status);
            Assert.Equal(8080, process.Port);

            CleanupApp(process);
        }

        [Fact]
        public async Task RunProject_will_add_all_variables_passed_to_process()
        {
            _webApp.Port = 8080;

            EnvironmentVariableDictionary varList = new EnvironmentVariableDictionary
            {
                { "foo", "bar" }
            };

            var process = await _service.Start(_webApp, _webApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(varList), CancellationToken.None);

            Thread.Sleep(500);

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Running, process.Status);
            Assert.Equal(8080, process.Port);
            Assert.True(process.Process.StartInfo.EnvironmentVariables.ContainsKey("foo"), "Environment variable \"foo\" has not been set.");
            Assert.Equal("bar", process.Process.StartInfo.EnvironmentVariables["foo"]);

            CleanupApp(process);
        }

        [Fact]
        public async Task InstallProject_returns_process()
        {
            _consoleApp.Source = "http://artifactory.org/nuget";

            var process = await _service.Install(_consoleApp, _consoleApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppTask.Install, process.Task);
            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Null(process.Port);
        }

        [Fact]
        public async Task InstallProject_sets_source_if_specified()
        {
            string source = "http://artifactory.org/nuget";
            _consoleApp.Source = source;

            var process = await _service.Install(_consoleApp, _consoleApp.GetAppDirectoryPaths(_fileSystem)[0], TestOptions.Create(), CancellationToken.None);

            Assert.Contains($"--source {source}", process.Process.StartInfo.Arguments, StringComparison.InvariantCulture);
        }

        [Theory]
        [InlineData(true, "taskkill", "/F /IM dotnet.exe")]
        [InlineData(false, "killall", "dotnet")]
        public async Task KillProcesses_kills_all_dotnet_processes(bool isWindows, string expectedFileName, string expectedArguments)
        {
            _runtimeInfoMock.Setup(m => m.IsWindows).Returns(isWindows).Verifiable();

            var process = await _service.Kill(TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Equal(AppTask.Kill, process.Task);

            var startInfo = process.Process.StartInfo;
            Assert.Equal(expectedFileName, startInfo.FileName);
            Assert.Equal(expectedArguments, startInfo.Arguments);

            _runtimeInfoMock.Verify();
        }

        [Fact]
        public async Task ShutdownBuildServer_shuts_down_all_build_servers()
        {
            _fileSystem.WorkingDirectory = PathHelper.RegiTestRootPath;

            var process =  await _service.ShutdownBuildServer(TestOptions.Create(), CancellationToken.None);

            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Equal(AppTask.Cleanup, process.Task);

            var startInfo = process.Process.StartInfo;
            Assert.Contains("dotnet", startInfo.FileName, StringComparison.InvariantCulture);
            Assert.Equal(FrameworkCommands.DotnetCore.ShutdownBuildServer, startInfo.Arguments);
        }

        [Fact]
        public void BuildCommand_does_not_add_command_options_if_restoring()
        {
            var project = SampleProjects.Backend;
            project.Arguments.AddOptions("*", "--dont-do-this-on-restore");
            project.Arguments.AddOptions("restore", "--foo bar");

            var command = ((Dotnet)_service).BuildCommandArguments(FrameworkCommands.DotnetCore.Restore, project, TestOptions.Create());

            Assert.Equal("restore --foo bar", command);
        }
    }
}
