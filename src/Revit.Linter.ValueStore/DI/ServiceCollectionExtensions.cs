using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Revit.Linter.ValueStore.Abstractions;
using Revit.Linter.ValueStore.Serialization;
using Revit.Linter.ValueStore.Services;

namespace Revit.Linter.ValueStore.DI;

public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddValueStore(Action<ValueStoreOptions> configure)
		{
			if (configure is null) throw new ArgumentNullException(nameof(configure));
			services.AddOptions<ValueStoreOptions>().Configure(configure);
			services.TryAddSingleton<ValueStoreNotificationHub>();
			services.TryAddSingleton<IValueStoreNotificationSource>(provider =>
				provider.GetRequiredService<ValueStoreNotificationHub>());
			services.TryAddSingleton<IValueStoreSerializer, YamlValueStoreSerializer>();
			services.TryAddSingleton(typeof(IValueStore<>), typeof(FileValueStore<>));
			return services;
		}

		public IServiceCollection AddValueStore<TSerializer>(Action<ValueStoreOptions> configure)
			where TSerializer : class, IValueStoreSerializer
		{
			if (configure is null) throw new ArgumentNullException(nameof(configure));
			services.AddOptions<ValueStoreOptions>().Configure(configure);
			services.TryAddSingleton<ValueStoreNotificationHub>();
			services.TryAddSingleton<IValueStoreNotificationSource>(provider =>
				provider.GetRequiredService<ValueStoreNotificationHub>());
			services.Replace(ServiceDescriptor.Singleton<IValueStoreSerializer, TSerializer>());
			services.TryAddSingleton(typeof(IValueStore<>), typeof(FileValueStore<>));
			return services;
		}
	}
}
