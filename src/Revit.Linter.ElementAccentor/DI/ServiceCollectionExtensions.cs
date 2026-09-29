using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementAccentor.Services;

namespace Revit.Linter.ElementAccentor.DI;

/// <summary>Registers reversible element accent operations and graphics override services.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds element accent services to the application service collection.</summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddElementAccentor() => services
           .AddSingleton<IAccentElementsService, SelectElementsService>()
           .AddSingleton<IAccentElementsService, ShowElementsService>()
           .AddSingleton<IAccentElementsService, IsolateElementsOnViewService>()
           .AddSingleton<IAccentElementsService, CutViewByElementsService>()
           .AddSingleton<IOverrideElementGraphicsService, OverrideElementGraphicsService>()
           .AddSingleton<IOverrideFilterGraphicsService, OverrideFilterGraphicsService>()
       ;
    }
}
