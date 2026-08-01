using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.ElementDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, ElementDiagnosticRegistrationProvider>();
        }
    }
}
