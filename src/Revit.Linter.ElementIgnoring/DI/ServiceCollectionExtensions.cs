using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementIgnoring.Abstractions.Services;
using Revit.Linter.ElementIgnoring.Services;

namespace Revit.Linter.ElementIgnoring.DI;

/// <summary>
/// Registers services for marking elements as ignored and detecting ignored elements.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared element-ignore manager as both a provider and a detector.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementIgnoringModule() => services
            .AddSingleton<IgnoreElementManager>()
            .AddSingleton<IIgnoreElementDetector>(i => i.GetRequiredService<IgnoreElementManager>())
            .AddSingleton<IIgnoreElementProvider>(i => i.GetRequiredService<IgnoreElementManager>())
        ;
    }
}
