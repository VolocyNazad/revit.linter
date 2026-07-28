using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ValueStore.Abstractions.Services;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics.DI;

public static class ServiceCollectionExtensions
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory,
        "collision.config.yaml"
    );

    extension(IServiceCollection services)
    {
        public IServiceCollection AddCollisionDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>()
                .AddSingleton<ElementFunctionFactory>()
                .AddSingleton<DocumentFilterFactory>()
                .AddSingleton<IGetElementGeometryService, GetElementGeometryService>()
                .AddSingleton<IGetElementBoundingBoxService, GetElementBoundingBoxService>();
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
                ElementDiagnosticId id = new(
                    rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
                services
                    .AddSingleton<IElementDiagnostic>(i =>
                    {
                        var diagnostic = new ElementDiagnostic(
                            i.GetRequiredService<ElementFilterFactory>(),
                            i.GetRequiredService<ElementFunctionFactory>(),
                            i.GetRequiredService<IGetElementBoundingBoxService>(),
                            i.GetRequiredService<IGetElementGeometryService>(),
                            i.GetRequiredService<IRevitTransactionMemoryCache>())
                        {
                            Identity = id,
                            TakeFormula = rule.AndTake,
                            GroupByFormula = rule.GroupBy,
                        };
                        return diagnostic;
                    })
                    .AddSingleton<IElementDiagnosticFilter>(i =>
                        new ElementDiagnosticFilter(i.GetRequiredService<ElementFilterFactory>())
                        {
                            Identity = id,
                            Formula = rule.Take
                        })
                    .AddSingleton<IElementDiagnosticDocumentFilter>(i =>
                        new ElementDiagnosticDocumentFilter(i.GetRequiredService<DocumentFilterFactory>())
                        {
                            Identity = id,
                            Formula = rule.TakeDocument
                        })
                    .AddSingleton(i => new ElementDiagnosticIdOverride(
                        id, i.GetRequiredService<IValueStore<ElementDiagnosticOverridesSettings>>()));
            }
        }
    }
}
