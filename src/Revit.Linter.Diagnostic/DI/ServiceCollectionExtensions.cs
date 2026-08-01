using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Diagnostic.Abstractions.Services;
using Revit.Linter.Diagnostic.Services;

namespace Revit.Linter.Diagnostic.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticModule() => services
            .AddSingleton<IDiagnosticCatalog, DiagnosticCatalog>()
            .AddSingleton<IDiagnosticService, DiagnosticService>()
        ;
    }
}
