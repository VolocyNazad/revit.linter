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
                .AddSingleton<ValueStoreNotificationHub>()
                .AddSingleton<IValueStoreNotificationSource>(provider =>
                    provider.GetRequiredService<ValueStoreNotificationHub>())
                .AddSingleton(typeof(IValueStore<>), typeof(YmlFileValueStore<>));
        }
    }
}
