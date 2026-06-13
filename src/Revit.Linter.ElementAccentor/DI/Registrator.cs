using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.Services;

namespace Revit.Linter.ElementAccentor.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementAccentor() => services
           .AddSingleton<IAccentElementsService, SelectElementsService>()
           .AddSingleton<IAccentElementsService, ShowElementsService>()
           .AddSingleton<IAccentElementsService, IsolateElementsOnViewService>()
           .AddSingleton<IAccentElementsService, CutViewByElementsService>()
       ;
    }
}
