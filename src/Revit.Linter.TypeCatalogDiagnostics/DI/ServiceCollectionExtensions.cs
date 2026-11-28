using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.TypeCatalogDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for type catalog diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the built-in type catalog diagnostic provider.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddTypeCatalogDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, TypeCatalogDiagnosticRegistrationProvider>();
        }
    }
}
