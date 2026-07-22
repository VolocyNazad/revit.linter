using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Services;

namespace Revit.Linter.ElementIgnoring.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementIgnoringModule() => services
            .AddSingleton<IgnoreElementManager>()
            .AddSingleton<IIgnoreElementDetector>(i => i.GetRequiredService<IgnoreElementManager>())
            .AddSingleton<IIgnoreElementProvider>(i => i.GetRequiredService<IgnoreElementManager>())
        ;
    }
}
