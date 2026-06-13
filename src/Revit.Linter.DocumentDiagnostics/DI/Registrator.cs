using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentDiagnostics.Infrastructure.Extensions;
using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics.DI;

public static class Registrator
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDocumentDiagnostics()
        {
            foreach (var id in DocumentDiagnosticIdCollector.GetAllDiagnosticIds())
                services
                    .AddSingleton(id)
                    .AddSingleton(i => new DocumentDiagnosticIdOverrides(id, id.DefaultSeverity, id.IsActive));

            return services
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IDocumentDiagnostic>().WithLifetime(ServiceLifetime.Singleton).Add()
                .From(Assembly.GetExecutingAssembly())
                    .FindImplementationsOf<IDocumentDiagnosticFilter>().WithLifetime(ServiceLifetime.Singleton).Add()
            ;
        }
    }
}
