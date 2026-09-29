using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementVisualization.Services;

namespace Revit.Linter.ElementVisualization.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementVisualization() => services
            .AddSingleton<IElementVisualizationPipelineFactory, ElementVisualizationPipelineFactory>();
    }
}
