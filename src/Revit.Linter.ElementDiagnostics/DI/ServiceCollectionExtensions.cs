using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;
using System.Reflection;

namespace Revit.Linter.ElementDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddElementDiagnostics()
        {
            string namespacePrefix = typeof(ElementDiagnosticIdCollector).Namespace!;

            foreach (var id in ElementDiagnosticIdCollector.GetAllDiagnosticIds())
                services
                    .AddSingleton(i => new ElementDiagnosticIdOverride(id, id.DefaultSeverity, id.IsActive));

            return services
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IElementDiagnostic>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IElementDiagnosticFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IElementDiagnosticDocumentFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IElementFix>().WithLifetime(ServiceLifetime.Singleton).Add()
            ;
        }
    }
}
