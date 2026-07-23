using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DocumentDiagnostics.Infrastructure.Extensions;
using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDocumentDiagnostics()
        {
            string namespacePrefix = typeof(DocumentDiagnosticIdCollector).Namespace!;

            foreach (var id in DocumentDiagnosticIdCollector.GetAllDiagnosticIds())
                services
                    .AddSingleton(i => new DocumentDiagnosticIdOverride(id, id.DefaultSeverity, id.IsActive));

            return services
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IDocumentDiagnostic>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IDocumentDiagnosticFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
            ;
        }
    }
}
