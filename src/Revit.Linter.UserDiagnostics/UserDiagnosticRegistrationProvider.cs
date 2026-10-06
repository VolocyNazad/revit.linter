using Revit.Linter.ConfigurationPath;
using Microsoft.Extensions.Logging;
using Revit.Linter.UserDiagnostics.Models;
using Revit.Linter.UserDiagnostics.Services;
using Toolkit.ValueStore.Abstractions;
using Revit.Linter.Languages.Factories;

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
    private static readonly string _configPath = ConfigurationPathUtils.ConfigPath;
    private static readonly string _exampleConfigPath = ConfigurationPathUtils.ExampleConfigPath;
    private static readonly string _practicalTourConfigPath = ConfigurationPathUtils.PracticalTourConfigPath;
    private readonly ConfigurationFileChangeSource _changeSource = new(
        [_configPath, _exampleConfigPath, _practicalTourConfigPath]);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        List<(DiagnosticRule Rule, bool IsExample, bool IsTour, string ConfigurationPath)> loadedRules = [];
        HashSet<string> codes = new(StringComparer.Ordinal);
        Exception? configurationError = null;
        string? configurationErrorPath = null;
        foreach ((string path, bool isExample, bool isTour) in new[]
                  {
                      (_configPath, false, false), (_exampleConfigPath, true, false), (_practicalTourConfigPath, false, true),
                  })
        {
            bool loaded = ConfigurationPathUtils.TryGetConfigurations(
                path, out List<DiagnosticRule>? rules, out Exception? error);
            if (!loaded)
            {
                configurationError = error;
                configurationErrorPath = path;
                continue;
            }

            foreach (DiagnosticRule rule in (rules ?? []).Where(rule => codes.Add(rule.Code)))
                loadedRules.Add((rule, isExample, isTour, path));
        }

        if (configurationError is null)
            configurationErrorState.Clear();
        else if (configurationErrorState.Set(configurationError))
            logger.LogError(
                configurationError,
                "Failed to parse user diagnostic configuration {ConfigurationPath}; treating it as empty",
                configurationErrorPath);
        foreach ((DiagnosticRule rule, bool isExample, bool isTour, string configurationPath) in loadedRules)
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription, isExample, isTour);
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
                    .ToArray(),
                configurationPath)
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
