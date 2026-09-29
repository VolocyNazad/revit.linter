using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.ThemeManaging.Services;

namespace Revit.Linter.ThemeManaging.DI;

/// <summary>
/// Registers application theme management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the shared theme service to the application service collection.
        /// </summary>
        /// <returns>The same service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddThemeManagingModule()
            => services.AddSingleton<IThemeService, ThemeService>();
    }
}
