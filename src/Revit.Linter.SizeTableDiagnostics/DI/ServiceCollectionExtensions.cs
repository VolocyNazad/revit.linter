using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.SizeTableDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for size lookup table diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the size lookup table diagnostic provider.
        /// </summary>
        /// <returns>The service collection for registration chaining.</returns>
        public IServiceCollection AddSizeTableDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, SizeTableDiagnosticRegistrationProvider>();
        }
    }
}
