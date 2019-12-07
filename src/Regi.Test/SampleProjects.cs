﻿using Regi.Models;
using Regi.Test.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Regi.Test
{
    public static class SampleProjects
    {
        public static Project ClassLib => new Project
        {
            Name = "ClassLib",
            Paths = new[] { PathHelper.GetSampleProjectPath("ClassLib") },
            Type = ProjectType.Web, // TODO: have a classlib type
        };

        public static Project Frontend => new Project
        {
            Name = "Frontend",
            Path = PathHelper.GetSampleProjectPath("Frontend"),
            Framework = ProjectFramework.Node,
            Type = ProjectType.Web,
            Port = 3000,
            Commands = new Dictionary<string, string>
            {
                { "start", "run dev" }
            }
        };

        public static Project Backend => new Project
        {
            Name = "Backend",
            Path = PathHelper.GetSampleProjectPath("Backend"),
            Framework = ProjectFramework.Dotnet,
            Type = ProjectType.Web,
            Port = 5000,
            Arguments = new CommandDictionary
            {
                { "*", new List<string> { "--foo bar" } }
            }
        };

        public static Project XunitTests => new Project
        {
            Name = "SampleSuccessfulTests",
            Path = PathHelper.GetSampleProjectPath("SampleSuccessfulTests"),
            Framework = ProjectFramework.Dotnet,
            Type = ProjectType.Unit
        };

        public static Project JestTests => new Project
        {
            Name = "NodeApp",
            Path = PathHelper.GetSampleProjectPath("NodeApp/package.json"),
            Framework = ProjectFramework.Node
        };

        public static Project IntegrationTests => new Project
        {
            Name = "IntegrationTests",
            Path = PathHelper.GetSampleProjectPath("IntegrationTests/package.json"),
            Framework = ProjectFramework.Node,
            Requires = new List<string> { Frontend.Name, Backend.Name },
            RawOutput = true,
            Serial = true,
            Port = 5000,
            Environment = new Dictionary<string, object>
            {
                { "HEADLESS", bool.FalseString }
            }
        };

        public static Project AppCollection => new Project
        {
            Name = "AppCollection",
            Paths = new List<string>
            {
                PathHelper.GetSampleProjectPath("AppCollection/App1/App1.csproj"),
                PathHelper.GetSampleProjectPath("AppCollection/App2/App2.csproj"),
                PathHelper.GetSampleProjectPath("AppCollection/App3/App3.csproj"),
            },
            Framework = ProjectFramework.Dotnet,
            Type = ProjectType.Web
        };

        public static Project TestCollection => new Project
        {
            Name = "TestCollection",
            Paths = new List<string>
            {
                PathHelper.GetSampleProjectPath("TestCollection/Test1/Test1.csproj"),
                PathHelper.GetSampleProjectPath("TestCollection/Test2/Test2.csproj"),
                PathHelper.GetSampleProjectPath("TestCollection/Test3/Test3.csproj"),
            },
            Framework = ProjectFramework.Dotnet,
            Type = ProjectType.Unit
        };

        public static Project SimpleNodeApp => new Project
        {
            Name = "NodeApp",
            Path = PathHelper.GetSampleProjectPath("NodeApp/package.json"),
            Type = ProjectType.Web,
            Framework = ProjectFramework.Node,
            Port = 9081
        };

        public static StartupConfig ConfigurationDefault => new StartupConfig
        {
            Apps = new List<Project>
            {
                Frontend, Backend
            },
            Tests = new List<Project>
            {
                XunitTests, JestTests, IntegrationTests
            },
            Services = new List<Project>()
        };

        public static StartupConfig ConfigurationGood => new StartupConfig
        {
            Apps = new List<Project>
            {
                new Project
                {
                    Name = "SampleApp1",
                    Path = PathHelper.GetSampleProjectPath("SampleApp/SampleApp.csproj"),
                    Type = ProjectType.Web
                },
                new Project
                {
                    Name = "SampleApp2",
                    Path = PathHelper.GetSampleProjectPath("SampleApp/SampleApp.csproj"),
                    Type = ProjectType.Web,
                    Port = 9080,
                    Serial = true
                },
                SimpleNodeApp
            },
            Tests = new List<Project>
            {
                new Project
                {
                    Name = "SampleSuccessfulTests",
                    Path = PathHelper.GetSampleProjectPath("SampleSuccessfulTests/SampleSuccessfulTests.csproj"),
                    Type = ProjectType.Unit
                },
                new Project
                {
                    Name = "SampleSuccessfulTests",
                    Path = PathHelper.GetSampleProjectPath("SampleSuccessfulTests/SampleSuccessfulTests.csproj"),
                    Type = ProjectType.Integration,
                    Serial = true
                }
            },
            Services = new List<Project>(),
            RawSources = new Dictionary<string, string>
            {
                { ProjectFramework.Dotnet.ToString(), "http://nuget.org/api" },
                { ProjectFramework.Node.ToString(), "http://npmjs.org" }
            }
        };
    }
}
