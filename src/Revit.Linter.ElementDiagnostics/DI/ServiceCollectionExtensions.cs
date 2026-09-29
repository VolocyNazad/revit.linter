using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.ElementDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for element diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the built-in element diagnostic provider.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddElementDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, ElementDiagnosticRegistrationProvider>();
        }
    }
}
