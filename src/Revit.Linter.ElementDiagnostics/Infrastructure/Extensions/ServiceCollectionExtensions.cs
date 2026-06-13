using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public AssemblySelector From(Assembly assembly)
        {
            return new AssemblySelector(services, assembly);
        }
    }

    public class AssemblySelector(IServiceCollection services, Assembly assembly)
    {
        public ImplementationSelector FindImplementationsOf<TInterface>()
            => new(services, assembly, typeof(TInterface));
    }

    public class ImplementationSelector(IServiceCollection services, Assembly assembly, Type interfaceType)
    {
        private ServiceLifetime _serviceLifetime = ServiceLifetime.Scoped;

        public ImplementationSelector WithLifetime(ServiceLifetime lifetime)
        {
            _serviceLifetime = lifetime;
            return this;
        }

        public IServiceCollection Add()
        {
            foreach (var implementation in GetImplementations().ToList())
            {
                services.Add(new ServiceDescriptor(interfaceType, implementation, _serviceLifetime));
            }

            return services;
        }

        public IServiceCollection TryAdd()
        {
            foreach (var implementation in GetImplementations().ToList())
            {
                services.TryAdd(new ServiceDescriptor(interfaceType, implementation, _serviceLifetime));
            }

            return services;
        }

        private IEnumerable<Type> GetImplementations()
            => assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && interfaceType.IsAssignableFrom(t));
    }
}
