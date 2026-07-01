using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.UserDiagnostics.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Revit.Linter.UserDiagnostics.DI;

public static class Registrator
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory,
        "config.yaml"
    );

    extension(IServiceCollection services)
    {
        public IServiceCollection AddUserDiagnostics()
        {
            services.AddSingleton<ElementFilterFactory>()
                .AddSingleton<ElementFunctionFactory>()
                .AddSingleton<DocumentFilterFactory>();
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
    }
}
