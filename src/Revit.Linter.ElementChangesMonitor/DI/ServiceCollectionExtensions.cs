using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Monitor = Revit.Linter.ElementChangesMonitor.Services.ElementChangesMonitor;

namespace Revit.Linter.ElementChangesMonitor.DI;

/// <summary>
/// Registers element-change monitoring services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the element-change monitor to the application service collection.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementChangesMonitorModule() => services
           .AddSingleton<IElementChangesMonitor, Monitor>()
       ;
    }
}
