﻿using Regi.Models;
using Regi.Services;
using Regi.Test.Helpers;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Services
{
    public class DotnetServiceTests
    {
        private readonly TestConsole _console;
        private readonly IDotnetService _service;

        private readonly Project _successfulTests;
        private readonly Project _failedTests;
        private readonly Project _application;
        private readonly Project _applicationError;
        private readonly Project _applicationLong;

        public DotnetServiceTests(ITestOutputHelper testOutput)
        {
            _console = new TestConsole(testOutput);
            _service = new DotnetService(_console);

            _successfulTests = new Project("SampleSuccessfulTests", new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles("SampleSuccessfulTests.csproj", SearchOption.AllDirectories)
                .First()
                .FullName);
            _failedTests = new Project("SampleFailedTests", new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles("SampleFailedTests.csproj", SearchOption.AllDirectories)
                .First()
                .FullName);
            _application = new Project("SampleApp", new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles("SampleApp.csproj", SearchOption.AllDirectories)
                .First()
                .FullName);
            _applicationError = new Project("SampleAppError", new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles("SampleAppError.csproj", SearchOption.AllDirectories)
                .First()
                .FullName);
            _applicationLong = new Project("SampleAppLong", new DirectoryInfo(Directory.GetCurrentDirectory())
                .GetFiles("SampleAppLong.csproj", SearchOption.AllDirectories)
                .First()
                .FullName);
        }

        [Fact]
        public void TestProject_on_success_returns_status()
        {
            AppProcess unitTest = _service.TestProject(_successfulTests);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Success, unitTest.Status);
        }

        [Fact]
        public void TestProject_verbose_on_success_prints_all_output()
        {
            AppProcess unitTest = _service.TestProject(_successfulTests, true);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Success, unitTest.Status);
            Assert.NotEmpty(_console.LogOutput);
        }

        [Fact]
        public void TestProject_on_failure_prints_only_exception_info()
        {
            AppProcess unitTest = _service.TestProject(_failedTests);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Failure, unitTest.Status);
            Assert.Contains("Test Run Failed.", _console.LogOutput);
        }

        [Fact]
        public void TestProject_verbose_on_failure_prints_all_output()
        {
            AppProcess unitTest = _service.TestProject(_failedTests, true);

            Assert.Equal(AppTask.Test, unitTest.Task);
            Assert.Equal(AppStatus.Failure, unitTest.Status);
            Assert.Contains("Total tests:", _console.LogOutput);
        }

        [Fact]
        public void RunProject_changes_status_from_running_to_success_on_exit()
        {
            AppProcess process = _service.RunProject(_application);

            Assert.Equal(AppStatus.Running, process.Status);

            process.Process.WaitForExit();

            Assert.Equal(AppStatus.Success, process.Status);
        }

        [Fact]
        public void RunProject_returns_failure_status_on_thrown_exception()
        {
            AppProcess process = _service.RunProject(_applicationError);

            process.Process.WaitForExit();

            Assert.Equal(AppStatus.Failure, process.Status);
        }

        [Fact]
        public void RunProject_starts_and_prints_nothing()
        {
            AppProcess process = _service.RunProject(_application);

            process.Process.WaitForExit();

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Null(_console.LogOutput);
        }

        [Fact]
        public void RunProject_verbose_starts_and_prints_all_output()
        {
            AppProcess process = _service.RunProject(_application, true);

            process.Process.WaitForExit();

            Assert.Equal(AppTask.Start, process.Task);
            Assert.Equal(AppStatus.Success, process.Status);
            Assert.Contains("Hello World!", _console.LogOutput);
        }

        [Fact]
        public void RunProject_long_starts_and_prints_nothing()
        {
            using (AppProcess app = _service.RunProject(_applicationLong, true))
            {
                Thread.Sleep(1000);

                Assert.Equal(AppTask.Start, app.Task);
                Assert.Equal(AppStatus.Running, app.Status);
            }
        }

        [Fact]
        public void RunProject_will_start_custom_port_if_specified()
        {
            using (AppProcess app = _service.RunProject(_applicationLong, true, 8080))
            {
                Thread.Sleep(1000);

                Assert.Equal(AppTask.Start, app.Task);
                Assert.Equal(AppStatus.Running, app.Status);
                Assert.Equal(8080, app.Port);
            }
        }

        [Fact]
        public void RestoreProject_returns_process()
        {
            using (AppProcess process = _service.RestoreProject(_application, true))
            {
                Assert.Equal(AppTask.Install, process.Task);
                Assert.Equal(AppStatus.Success, process.Status);
                Assert.Null(process.Port);
            }
        }
    }
}
