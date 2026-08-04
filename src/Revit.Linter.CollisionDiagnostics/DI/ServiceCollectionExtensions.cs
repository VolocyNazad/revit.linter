using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;

namespace Revit.Linter.CollisionDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCollisionDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>()
                .AddSingleton<ElementFunctionFactory>()
                .AddSingleton<DocumentFilterFactory>()
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
