﻿using Moq;
using Regi.Models;
using Regi.Services;
using Regi.Test.Helpers;
using System.Collections.Generic;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Services
{
    public class ParallelServiceTests
    {
        private readonly IParallelService _service;
        private readonly Mock<INetworkingService> _networkingServiceMock;
        private readonly TestConsole _console;

        public ParallelServiceTests(ITestOutputHelper output)
        {
            _networkingServiceMock = new Mock<INetworkingService>();

            _console = new TestConsole(output);
            _service = new ParallelService(new TestConsole(output), _networkingServiceMock.Object);
        }

        [Fact]
        public void Queue_adds_to_parallel_by_default_and_serial_if_specified()
        {
            int taskCount = 5;
            int parallelExecutions = 0;
            int serialExecutions = 0;

            for (int i = 0; i < taskCount; i++)
            {
                _service.Queue(false, () =>
                {
                    parallelExecutions++;
                });

                _service.Queue(true, () =>
                {
                    serialExecutions++;
                });
            }

            _service.RunAll();

            Assert.Equal(taskCount, parallelExecutions);
            Assert.Equal(taskCount, ((ParallelService)_service).ParallelActions.Count);

            Assert.Equal(taskCount, serialExecutions);
            Assert.Equal(taskCount, ((ParallelService)_service).SerialActions.Count);
        }

        [Fact]
        public void QueueParallel_can_add_and_run_all_tasks()
        {
            int taskCount = 5;
            int executions = 0;

            for (int i = 0; i < taskCount; i++)
            {
                _service.QueueParallel(() =>
                {
                    executions++;
                });
            }

            _service.RunAll();

            Assert.Equal(taskCount, executions);
            Assert.Equal(taskCount, ((ParallelService)_service).ParallelActions.Count);
        }

        [Fact]
        public void QueueSerial_can_add_and_run_all_tasks()
        {
            int taskCount = 5;
            int executions = 0;

            for (int i = 0; i < taskCount; i++)
            {
                _service.QueueSerial(() =>
                {
                    executions++;
                });
            }

            _service.RunAll();

            Assert.Equal(taskCount, executions);
            Assert.Equal(taskCount, ((ParallelService)_service).SerialActions.Count);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Queue_adds_and_runs_serial_actions_after_parallel_actions(int taskCount)
        {
            int parallelRunCount = 0;
            int serialRunCount = 0;

            _service.QueueParallel(() => parallelRunCount++);
            _service.QueueParallel(() => parallelRunCount++);

            for (int i = 0; i < taskCount; i++)
            {
                _service.QueueSerial(() => serialRunCount++);
            }

            _service.RunAll();

            Assert.Equal(2, parallelRunCount);
            Assert.Equal(taskCount, serialRunCount);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void WaitOnPorts_waits_until_all_required_ports_are_being_listened_on(int expectedCallCount)
        {
            int callCount = 0;
            _networkingServiceMock.Setup(m => m.GetListeningPorts())
                .Callback(() => callCount++)
                .Returns(() =>
                {
                    if (callCount < expectedCallCount)
                        return new IPEndPoint[] { };
                    else
                        return new IPEndPoint[] { new IPEndPoint(00000, 9080) };
                })
                .Verifiable();

            _service.WaitOnPorts(new List<Project> { new Project { Name = "TestProject", Port = 9080 } });

            _networkingServiceMock.Verify(m => m.GetListeningPorts(), Times.Exactly(expectedCallCount));
            Assert.Equal(callCount, expectedCallCount);
        }
    }
}
