using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.UserDiagnostics.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.UserDiagnostics.DI;

public static class Registrator
{
    private static string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory,
        "config.yaml"
    );

    extension(IServiceCollection services)
    {
        public IServiceCollection AddUserDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>();
            services.AddSingleton<ElementFunctionFactory>();
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
            ElementDiagnosticId id = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
            services
                .AddSingleton(i => id)
                .AddSingleton<IElementDiagnostic>(i =>
                    new ElementDiagnostic(
                        i.GetRequiredService<ElementFunctionFactory>())
                    {
                        Identity = id,
                        Formula = rule.Check
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
