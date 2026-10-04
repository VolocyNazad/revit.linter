using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Languages.DI;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

namespace Revit.Linter.CollisionDiagnostics.DI;

/// <summary>
/// Provides dependency-injection registration for collision diagnostics.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers collision diagnostics and their supporting services.
        /// </summary>
        /// <returns>The service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddCollisionDiagnostics()
        {
            services.AddFormulaFactories()
                .AddSingleton<IGetElementGeometryService, GetElementGeometryService>()
                .AddSingleton<IGetElementBoundingBoxService, GetElementBoundingBoxService>()
                .AddSingleton<CollisionDiagnosticRegistrationProvider>()
                .AddSingleton<IDiagnosticRegistrationProvider>(provider =>
                    provider.GetRequiredService<CollisionDiagnosticRegistrationProvider>())
                .AddSingleton<IDiagnosticCatalogChangeSource>(provider =>
                    provider.GetRequiredService<CollisionDiagnosticRegistrationProvider>());
            return services;
        }
    }
}
