using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.CollisionTestModels.Abstractions.Services;
using Revit.Linter.CollisionTestModels.Infrastructure.Services;

namespace Revit.Linter.CollisionTestModels.DI;

/// <summary>
/// Provides dependency-injection registration for collision-testing document tooling.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the collision-testing document service.
        /// </summary>
        /// <returns>The service collection so that additional registrations can be chained.</returns>
        public IServiceCollection AddCollisionTestModels()
        {
            services.AddSingleton<ICollisionTestModelService, CollisionTestModelService>();
            return services;
        }
    }
}
