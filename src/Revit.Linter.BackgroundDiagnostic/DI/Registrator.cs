using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.BackgroundDiagnostic.Abstractions.Services;
using Revit.Linter.BackgroundDiagnostic.Services;

namespace Revit.Linter.BackgroundDiagnostic.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBackgroundDiagnosticModule() => services
            .AddSingleton<IBackgroundDiagnosticService, BackgroundDiagnosticService>()
        ;
    }
}
