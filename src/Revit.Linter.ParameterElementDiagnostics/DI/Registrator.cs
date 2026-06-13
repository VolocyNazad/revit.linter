using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.TransactionMemoryCache.Abstractions.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.ParameterElementDiagnostics.DI;

public static class Registrator
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
            DocumentDiagnosticId id = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
            services
                .AddSingleton(i => id)
                 .AddSingleton<IDocumentDiagnostic>(i =>
                    new DocumentDiagnostic(
                        i.GetRequiredService<IRevitTransactionMemoryCache>())
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
                .AddSingleton(i => new DocumentDiagnosticIdOverrides(id, id.DefaultSeverity, id.IsActive));
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
