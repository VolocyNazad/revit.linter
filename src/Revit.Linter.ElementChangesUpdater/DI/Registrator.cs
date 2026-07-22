using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementChangesMonitor.Abstractions.Services;
using Monitor = Revit.Linter.ElementChangesMonitor.Services.ElementChangesMonitor;

namespace Revit.Linter.ElementChangesMonitor.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementChangesMonitorModule() => services
           .AddSingleton<IElementChangesMonitor, Monitor>()
       ;
    }
}
