using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Presentation.Abstractions.Services;
using Revit.Linter.Presentation.Services;

namespace Revit.Linter.Presentation.DI;

/// <summary>
/// Registers the shared presentation services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the service that remembers and applies the column visibility of the linter tables.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddTableColumnsModule()
            => services.AddSingleton<ITableColumnsService, TableColumnsService>();
    }
}
