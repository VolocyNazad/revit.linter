using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
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
    UserDiagnosticSkippedRuleState skippedRuleState,
    ILogger<UserDiagnosticRegistrationProvider> logger)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = ConfigurationPathUtils.ConfigPath;
    private static readonly string _debugConfigPath = ConfigurationPathUtils.DebugConfigPath;
    private static readonly string _exampleConfigPath = ConfigurationPathUtils.ExampleConfigPath;
    private readonly ConfigurationFileChangeSource _changeSource = new(
        [_configPath, _debugConfigPath, _exampleConfigPath]);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        List<(DiagnosticRule Rule, bool IsExample, bool IsDebug, string ConfigurationPath)> loadedRules = [];
        List<string> skippedRules = [];
        HashSet<string> codes = new(StringComparer.Ordinal);
        Exception? configurationError = null;
        string? configurationErrorPath = null;
        foreach ((string path, bool isExample, bool isDebug) in new[]
                  {
                      (_configPath, false, false), (_debugConfigPath, false, true),
                      (_exampleConfigPath, true, false),
                  })
        {
            // Debug configurations are optional and user-owned: reading a missing file must not
            // create it, otherwise every machine grows an empty debug folder on startup.
            if (isDebug && !File.Exists(path)) continue;
            bool loaded = ConfigurationPathUtils.TryGetVersionedConfigurations(
                path, out ConfigurationFile<DiagnosticRule>? file, out Exception? error);
            if (!loaded)
            {
                configurationError = error;
                configurationErrorPath = path;
                continue;
            }
            if (file is not null && file.Version > ConfigurationPathUtils.SupportedConfigurationVersion)
            {
                logger.LogError(
                    "Diagnostic configuration {ConfigurationPath} has version {ConfigurationVersion}, " +
                    "newer than supported version {SupportedVersion}; ignoring the file",
                    path, file.Version, ConfigurationPathUtils.SupportedConfigurationVersion);
                continue;
            }

            foreach (DiagnosticRule rule in (file?.Rules ?? []).Where(rule => codes.Add(rule.Code)))
                loadedRules.Add((rule, isExample, isDebug, path));
        }

        if (configurationError is null)
            configurationErrorState.Clear();
        else if (configurationErrorState.Set(configurationError))
            logger.LogError(
                configurationError,
                "Failed to parse user diagnostic configuration {ConfigurationPath}; treating it as empty",
                configurationErrorPath);
        foreach ((DiagnosticRule rule, bool isExample, bool isDebug, string configurationPath) in loadedRules)
        {
            ElementDiagnosticRegistration? registration = TryCreateRegistration(
                rule, isExample, isDebug, configurationPath);
            if (registration is null)
                skippedRules.Add($"rule '{rule.Code}' from {configurationPath}");
            else
                yield return registration;
        }

        skippedRuleState.Set(skippedRules.Count == 0
            ? null
            : $"User rules skipped: {string.Join(", ", skippedRules)}.");
    }

    // A rule with invalid fix or visualization pipelines must not fail the whole catalog:
    // it is skipped and described through the skipped-rule state instead.
    private ElementDiagnosticRegistration? TryCreateRegistration(
        DiagnosticRule rule, bool isExample, bool isDebug, string configurationPath)
    {
        try
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description,
                rule.Message ?? UserDiagnosticLocalizations.GetString("Default_message"),
                rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription, isExample, isTour: false, isDebug: isDebug);
            return new ElementDiagnosticRegistration(
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
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Skipping user rule {RuleCode} from {ConfigurationPath}: its fix or visualization pipelines are invalid",
                rule.Code, configurationPath);
            return null;
        }
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];

    internal static T[] CreateFixes<T>(
        DiagnosticRule rule,
        Func<string, IReadOnlyList<ElementFixStepDefinition>, T> factory) => rule.Fixes
            .Select(pipeline => factory(pipeline.Name, pipeline.Steps))
            .ToArray();
}
