using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrasructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
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
            services.AddSingleton<ElementFilterFactory>();
            services.AddSingleton<ElementFunctionFactory>();
            services.AddSingleton<DocumentFilterFactory>();
            services.AddSingleton<IGetElementGeomentryService, GetElementGeomentryService>();
            services.AddSingleton<IGetElementBoundingBoxService, GetElementBoundingBoxService>();
            RegisterDiagnosticsUsingConfig(services);
            return services;
        }
    }

    private static void RegisterDiagnosticsUsingConfig(IServiceCollection services)
    {
        IDeserializer deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        string configContent = File.ReadAllText(GetConfigPath());
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
                    new ElementDiagnosticFilter(
                            i.GetRequiredService<ElementFilterFactory>())
                    {
                        Identity = id,
                        Formula = rule.Take
                    })
                .AddSingleton<IElementDiagnosticDocumentFilter>(i =>
                    new ElementDiagnosticDocumentFilter(
                            i.GetRequiredService<DocumentFilterFactory>())
                    {
                        Identity = id,
                        Formula = rule.TakeDocument
                    })
                .AddSingleton(i => new ElementDiagnosticIdOverrides(id, id.DefaultSeverity, id.IsActive));
            ;
        }
    }
    private static string GetConfigPath()
    {
        string? directoryPath = Path.GetDirectoryName(_configPath)
            ?? throw new InvalidOperationException("Diagnostic configuration file not found.");
        Directory.CreateDirectory(directoryPath);

        if (!File.Exists(_configPath))
            File.WriteAllText(_configPath, string.Empty);

        return _configPath;
    }
}
