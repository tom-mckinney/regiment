﻿using Microsoft.Extensions.DependencyInjection;
using Regi.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Regi.Services
{
    public interface IFrameworkServiceProvider
    {
        IFrameworkService GetFrameworkService(ProjectFramework framework);
    }

    public class FrameworkServiceProvider : IFrameworkServiceProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public FrameworkServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IFrameworkService GetFrameworkService(ProjectFramework framework)
        {
            switch (framework)
            {
                case ProjectFramework.Dotnet:
                    return _serviceProvider.GetRequiredService<IDotnetService>();
                case ProjectFramework.Node:
                    return _serviceProvider.GetRequiredService<INodeService>();
                case ProjectFramework.Any:
                    throw new ArgumentException($"Cannot get framework service for project framework type of {framework}", nameof(framework));
                default:
                    throw new NotImplementedException($"There is no implementation for framework of type {framework}");
            }
        }
    }
}
