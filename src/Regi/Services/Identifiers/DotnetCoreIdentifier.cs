﻿using Regi.Models;
using System;
using System.Threading.Tasks;

namespace Regi.Services.Identifiers
{
    public class DotnetCoreIdentifier : BaseIdentifier
    {
        private int Port = 8080;

        public DotnetCoreIdentifier(IFileSystem fileSystem) : base(fileSystem)
        {
        }

        public override ValueTask<bool> ShouldIdentify(Project project, IFileSystemDictionary directoryContents)
        {
            return new ValueTask<bool>(true);
        }

        public override ValueTask<bool> IsMatchAsync(Project project, IFileSystemDictionary directoryContents)
        {
            if (project?.Framework == ProjectFramework.Dotnet)
            {
                return new ValueTask<bool>(true);
            }

            if (directoryContents?.Count > 0)
            {
                foreach (var path in directoryContents.Keys)
                {
                    if (path.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new ValueTask<bool>(true);
                    }
                }
            }

            return new ValueTask<bool>(false);
        }

        public override async ValueTask<Project> CreateOrModifyAsync(Project project, IFileSystemDictionary directoryContents)
        {
            project = await base.CreateOrModifyAsync(project, directoryContents);

            project.Framework = ProjectFramework.Dotnet;

            if (project.Name.Contains("test", StringComparison.InvariantCultureIgnoreCase))
            {
                project.Type = ProjectType.Unit;
            }
            else
            {
                project.Type = ProjectType.Web;
                project.Port = Port;
            }

            Port++;

            return project;
        }
    }
}
