using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ParameterElementDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory,
        "parameter-element.config.yaml"
    );

    extension(IServiceCollection services)
    {
        public IServiceCollection AddParameterElementDiagnostics()
        {
            services.AddSingleton<DocumentFilterFactory>();
            RegisterDiagnosticsUsingConfig(services);
            return services;
        }

        private void RegisterDiagnosticsUsingConfig()
        {
            List<DiagnosticRule>? rules = ConfigurationPathUtils
                .GetConfigurations<List<DiagnosticRule>>(_configPath);
            if (rules is null) return;

            foreach (DiagnosticRule rule in rules)
            {
                DocumentDiagnosticId id = new(
                    rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
                services
                     .AddSingleton<IDocumentDiagnostic>(i =>
                        new DocumentDiagnostic(i.GetRequiredService<IRevitTransactionMemoryCache>())
                        {
                            Identity = id,
                            Parameters = rule.Parameters
                        })
                    .AddSingleton<IDocumentDiagnosticFilter>(i =>
                        new DocumentDiagnosticFilter(i.GetRequiredService<DocumentFilterFactory>())
                        {
                            Identity = id,
                            Formula = rule.Take
                        })
                    .AddSingleton(i => new DocumentDiagnosticIdOverride(id, id.DefaultSeverity, id.IsActive));
            }
        }
    }
}
