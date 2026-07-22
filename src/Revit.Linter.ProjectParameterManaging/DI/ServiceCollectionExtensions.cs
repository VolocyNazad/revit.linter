using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ProjectParameterManaging.Abstractions.Services;
using Revit.Linter.ProjectParameterManaging.Services;

namespace Revit.Linter.ProjectParameterManaging.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProjectParameterManagingModule() => services
            .AddSingleton<IProjectParameterProvider, ProjectParameterProvider>()
        ;
    }
}
