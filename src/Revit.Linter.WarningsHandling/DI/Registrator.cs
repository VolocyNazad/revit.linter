using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.WarningsHandling.Abstractions.Services;
using Revit.Linter.WarningsHandling.Services;

namespace Revit.Linter.WarningsHandling.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddRevitWarningsModule() => services
            .AddSingleton<IRevitWarningsService, RevitWarningsService>()
        ;
    }
}
