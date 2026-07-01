using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Diagnostic.Services;

namespace Revit.Linter.Diagnostic.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDiagnosticModule() => services
            .AddSingleton<IDiagnosticService, DiagnosticService>()
        ;
    }
}
