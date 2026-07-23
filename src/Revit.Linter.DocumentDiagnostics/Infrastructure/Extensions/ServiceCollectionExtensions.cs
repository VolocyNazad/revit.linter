using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics.Infrastructure.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public AssemblySelector From(Assembly assembly, string namespacePrefix)
        {
            return new AssemblySelector(services, assembly, namespacePrefix);
        }
    }

    public class AssemblySelector(IServiceCollection services, Assembly assembly, string namespacePrefix)
    {
        public ImplementationSelector FindImplementationsOf<TInterface>()
            => new(services, assembly, namespacePrefix, typeof(TInterface));
    }

    public class ImplementationSelector(
        IServiceCollection services,
        Assembly assembly,
        string namespacePrefix,
        Type interfaceType)
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
            .Where(t => t.IsClass
                && !t.IsAbstract
                && t.Namespace?.StartsWith(namespacePrefix + ".", StringComparison.Ordinal) == true
                && interfaceType.IsAssignableFrom(t));
    }
}
