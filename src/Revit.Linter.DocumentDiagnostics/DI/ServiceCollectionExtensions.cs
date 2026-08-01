using Microsoft.Extensions.DependencyInjection;

namespace Revit.Linter.DocumentDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDocumentDiagnostics()
        {
            return services
                .AddSingleton<IDiagnosticRegistrationProvider, DocumentDiagnosticRegistrationProvider>();
        }
    }
}
