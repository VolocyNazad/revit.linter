using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ThemeManaging.Abstractions.Services;
using Revit.Linter.ThemeManaging.Services;

namespace Revit.Linter.ThemeManaging.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddThemeManagingModule()
            => services.AddSingleton<IThemeService, ThemeService>();
    }
}
