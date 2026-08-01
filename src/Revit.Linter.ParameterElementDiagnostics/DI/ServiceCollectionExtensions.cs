using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.ParameterElementDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddParameterElementDiagnostics()
        {
            services.AddSingleton<DocumentFilterFactory>()
                .AddSingleton<IDiagnosticRegistrationProvider, ParameterElementDiagnosticRegistrationProvider>();
            return services;
        }
    }
}
