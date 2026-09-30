using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementFixing.Abstractions.Services;
using Revit.Linter.ElementFixing.Services;

namespace Revit.Linter.ElementFixing.DI;

/// <summary>Registers configuration-driven element fix pipelines.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the element fix pipeline factory and built-in step executors.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementFixing() => services
            .AddSingleton<IElementFixStepExecutor, DeleteElementFixStepExecutor>()
            .AddSingleton<IElementFixPipelineFactory, ElementFixPipelineFactory>();
    }
}
