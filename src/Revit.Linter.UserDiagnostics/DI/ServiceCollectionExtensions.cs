using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.UserDiagnostics.Models;

namespace Revit.Linter.UserDiagnostics.DI;

public static class ServiceCollectionExtensions
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
            List<DiagnosticRule>? rules = ConfigurationPathUtils
                .GetConfigurations<List<DiagnosticRule>>(_configPath);
            if (rules is null) return;

            foreach (DiagnosticRule rule in rules)
            {
                ElementDiagnosticId id = new(
                    rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive, rule.IsObsolete, rule.ObsoleteDescription);
                services
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
                .AddSingleton(i => new ElementDiagnosticIdOverride(id, id.DefaultSeverity, id.IsActive));
            }
        }
    }
}
