using Revit.Linter.ConfigurationPath;
using Microsoft.Extensions.Logging;
using Revit.Linter.UserDiagnostics.Models;
using Revit.Linter.UserDiagnostics.Services;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.UserDiagnostics;

internal sealed class UserDiagnosticRegistrationProvider(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    DocumentFilterFactory documentFilterFactory,
    IElementFixPipelineFactory fixPipelineFactory,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore,
    UserDiagnosticConfigurationErrorState configurationErrorState,
    ILogger<UserDiagnosticRegistrationProvider> logger)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = Path.Combine(ConfigurationPathUtils.Directory, "config.yaml");
    private readonly ConfigurationFileChangeSource _changeSource = new(_configPath);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        bool loaded = ConfigurationPathUtils.TryGetConfigurations(
            _configPath, out List<DiagnosticRule>? rules, out Exception? error);
        if (!loaded)
        {
            if (configurationErrorState.Set(error!))
                logger.LogError(
                    error,
                    "Failed to parse user diagnostic configuration {ConfigurationPath}; treating it as empty",
                    _configPath);
            yield break;
        }

        configurationErrorState.Clear();
        if (rules is null) yield break;

        foreach (DiagnosticRule rule in rules)
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription);
            yield return new ElementDiagnosticRegistration(
                identity,
                new ElementDiagnostic(elementFunctionFactory) { Identity = identity, Formula = rule.Check },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new ElementDiagnosticIdOverride(identity, overrideStore),
                CreateFixes(rule, (name, steps) => fixPipelineFactory.Create(identity, name, steps)),
                rule.Visualizations
                    .Select(pipeline => visualizationPipelineFactory.Create(
                        identity, pipeline.Name, pipeline.Steps))
                    .ToArray())
            {
                Documentation = new("User diagnostics", "Пользовательские проверки"),
            };
        }
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];

    internal static T[] CreateFixes<T>(
        DiagnosticRule rule,
        Func<string, IReadOnlyList<ElementFixStepDefinition>, T> factory) => rule.Fixes
            .Select(pipeline => factory(pipeline.Name, pipeline.Steps))
            .ToArray();
}
