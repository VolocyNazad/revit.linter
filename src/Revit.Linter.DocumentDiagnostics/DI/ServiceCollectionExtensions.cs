using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.DocumentDiagnostics.Infrastructure.Extensions;
using Revit.Linter.ValueStore.Abstractions.Services;
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
                    .AddSingleton(i => new DocumentDiagnosticIdOverride(
                        id, i.GetRequiredService<IValueStore<DocumentDiagnosticOverridesSettings>>()));

            return services
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IDocumentDiagnostic>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly(), namespacePrefix)
                    .FindImplementationsOf<IDocumentDiagnosticFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
            ;
        }
    }
}
