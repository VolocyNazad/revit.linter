using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementVisualization.Services;

namespace Revit.Linter.ElementVisualization.DI;

/// <summary>Registers diagnostic visualization pipeline composition services.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds the visualization pipeline factory to the application service collection.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementVisualization() => services
            .AddSingleton<IElementVisualizationPipelineFactory, ElementVisualizationPipelineFactory>();
    }
}
