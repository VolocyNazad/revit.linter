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
            foreach (var id in ElementDiagnosticIdCollector.GetAllDiagnosticIds())
                services
                    .AddSingleton(i => new ElementDiagnosticIdOverride(id, id.DefaultSeverity, id.IsActive));

            return services
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IElementDiagnostic>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IElementDiagnosticFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IElementDiagnosticDocumentFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IElementFix>().WithLifetime(ServiceLifetime.Singleton).Add()
            ;
        }
    }
}
