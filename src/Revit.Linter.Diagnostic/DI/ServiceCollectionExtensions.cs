using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Diagnostic.Services;

namespace Revit.Linter.Diagnostic.DI;

/// <summary>
/// Registers diagnostic catalog and execution services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the diagnostic catalog and execution services to the application service collection.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddDiagnosticModule() => services
            .AddSingleton<IDiagnosticCatalogSnapshotFactory, DiagnosticCatalogSnapshotFactory>()
            .AddSingleton<IDiagnosticCatalog, DiagnosticCatalog>()
            .AddSingleton<IDiagnosticService, DiagnosticService>()
        ;
    }
}
