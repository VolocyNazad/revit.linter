using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ValueStore.Abstractions.Services;
using Revit.Linter.ValueStore.Services;

namespace Revit.Linter.ValueStore.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddValueStoreModule()
        {
            return services
                .AddSingleton(typeof(IValueStore<>), typeof(YmlFileValueStore<>));
        }
    }
}
