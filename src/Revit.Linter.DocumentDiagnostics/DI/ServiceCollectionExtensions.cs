using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.DocumentDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for document diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the built-in document diagnostic provider.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddDocumentDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, DocumentDiagnosticRegistrationProvider>();
        }
    }
}
