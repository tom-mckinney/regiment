﻿using Moq;
using Regi.Services;
using Regi.Test.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Regi.Test.Services
{
    public class NetworkingServiceTests
    {
        private readonly TestConsole _console;
        private readonly Mock<IRuntimeInfo> _runtimeInfoMock = new Mock<IRuntimeInfo>(MockBehavior.Strict);

        public NetworkingServiceTests(ITestOutputHelper output)
        {
            _console = new TestConsole(output);
        }

        private INetworkingService CreateService()
        {
            return new NetworkingService(_console, _runtimeInfoMock.Object);
        }

        [Fact]
        public void IsPortListening_gets_list_of_all_active_ports()
        {
            _runtimeInfoMock.Setup(m => m.IsWindows).Returns(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            _runtimeInfoMock.Setup(m => m.IsWindowsLinux).Returns(false);
            _runtimeInfoMock.Setup(m => m.IsMac).Returns(RuntimeInformation.IsOSPlatform(OSPlatform.OSX));
            _runtimeInfoMock.Setup(m => m.NewLine).Returns(Environment.NewLine);

            int port = new Random().Next(10000, 30000);

            var listener = TcpListener.Create(port);

            listener.Start();

            var service = CreateService();

            Assert.True(service.IsPortListening(port));

            listener.Stop();
        }

        [Theory]
        [InlineData(8080, true)]
        [InlineData(8888, false)]
        [InlineData(53483, false)]
        public void ContainsPort_Mac_returns_true_if_port_is_listening(int port, bool isListening)
        {
            FileInfo responseFile = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "SystemResponses", "Mac", "MacNetstat.txt"));

            if (!responseFile.Exists)
                throw new FileNotFoundException("Could not find test file");

            _runtimeInfoMock.Setup(m => m.IsMac).Returns(true).Verifiable();
            _runtimeInfoMock.Setup(m => m.IsWindows).Returns(false).Verifiable();
            _runtimeInfoMock.Setup(m => m.NewLine).Returns("\n").Verifiable();

            string netstatResponse = File.ReadAllText(responseFile.FullName);

            NetworkingService service = (NetworkingService)CreateService();

            Assert.Equal(isListening, service.ContainsNetstatPort(netstatResponse, port));

            _runtimeInfoMock.Verify();
        }
    }
}
