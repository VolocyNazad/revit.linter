using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.Services;

namespace Revit.Linter.ProjectParameterManaging.DI;

/// <summary>
/// Registers project-parameter management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared project-parameter provider to the application service collection.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddProjectParameterManagingModule() => services
            .AddSingleton<IProjectParameterProvider, ProjectParameterProvider>()
        ;
    }
}
