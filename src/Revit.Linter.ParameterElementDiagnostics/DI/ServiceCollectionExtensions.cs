using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.ParameterElementDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for parameter element diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers parameter element diagnostics and their supporting services.
        /// </summary>
        /// <returns>The service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddParameterElementDiagnostics()
        {
            services.AddSingleton<DocumentFilterFactory>()
                .AddSingleton<ParameterElementDiagnosticRegistrationProvider>()
                .AddSingleton<IDiagnosticRegistrationProvider>(provider =>
                    provider.GetRequiredService<ParameterElementDiagnosticRegistrationProvider>())
                .AddSingleton<IDiagnosticCatalogChangeSource>(provider =>
                    provider.GetRequiredService<ParameterElementDiagnosticRegistrationProvider>());
            return services;
        }
    }
}
