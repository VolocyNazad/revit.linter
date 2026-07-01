using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
using Revit.TransactionMemoryCache.Abstractions.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.CollisionDiagnostics.DI;

public static class Registrator
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
                .AddSingleton<IGetElementGeomentryService, GetElementGeomentryService>()
                .AddSingleton<IGetElementBoundingBoxService, GetElementBoundingBoxService>();
            RegisterDiagnosticsUsingConfig(services);
            return services;
        }

        private void RegisterDiagnosticsUsingConfig()
        {
            IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            ConfigurationPathUtils.EnsureFileExists(_configPath);
            string configContent = File.ReadAllText(_configPath);
            if (string.IsNullOrEmpty(configContent)) return;
            List<DiagnosticRule> rules = deserializer.Deserialize<List<DiagnosticRule>>(configContent);

            foreach (DiagnosticRule rule in rules)
            {
                ElementDiagnosticId id = new(
                    rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
                services
                    .AddSingleton(i => id)
                    .AddSingleton<IElementDiagnostic>(i =>
                    {
                        var diagnostic = new ElementDiagnostic(
                            i.GetRequiredService<ElementFilterFactory>(),
                            i.GetRequiredService<ElementFunctionFactory>(),
                            i.GetRequiredService<IGetElementBoundingBoxService>(),
                            i.GetRequiredService<IGetElementGeomentryService>(),
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
                    .AddSingleton(i => new ElementDiagnosticIdOverrides(id, id.DefaultSeverity, id.IsActive));
            }
        }
    }
}
