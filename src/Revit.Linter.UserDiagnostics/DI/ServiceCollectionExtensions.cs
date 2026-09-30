using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.UserDiagnostics.Abstractions.Services;
using Revit.Linter.UserDiagnostics.Services;

namespace Revit.Linter.UserDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for user-defined diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers user-defined diagnostics and their supporting services.
        /// </summary>
        /// <returns>The service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddUserDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>()
                .AddSingleton<ElementFunctionFactory>()
                .AddSingleton<DocumentFilterFactory>()
                .AddSingleton<UserDiagnosticConfigurationErrorState>()
                .AddSingleton<IUserDiagnosticConfigurationErrorSource>(provider =>
                    provider.GetRequiredService<UserDiagnosticConfigurationErrorState>())
                .AddSingleton<UserDiagnosticRegistrationProvider>()
                .AddSingleton<IDiagnosticRegistrationProvider>(provider =>
                    provider.GetRequiredService<UserDiagnosticRegistrationProvider>())
                .AddSingleton<IDiagnosticCatalogChangeSource>(provider =>
                    provider.GetRequiredService<UserDiagnosticRegistrationProvider>());
            return services;
        }
    }
}
