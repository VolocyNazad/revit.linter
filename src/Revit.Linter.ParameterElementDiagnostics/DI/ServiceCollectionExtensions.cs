using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.ParameterElementDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
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
