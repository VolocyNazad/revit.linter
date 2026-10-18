using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.ParameterElementDiagnostics.Services;
using Toolkit.ValueStore.Abstractions;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class ParameterElementDiagnosticRegistrationProvider(
    DocumentFilterFactory documentFilterFactory,
    IDocumentQueryService documentQueries,
    IValueStore<DocumentDiagnosticOverridesSettings> overrideStore,
    ParameterConfigurationErrorState configurationErrorState,
    ILogger<ParameterElementDiagnosticRegistrationProvider> logger)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = ConfigurationPathUtils.ParameterElementConfigPath;
    private static readonly string _debugConfigPath = ConfigurationPathUtils.DebugParameterElementConfigPath;
    private static readonly string _exampleConfigPath = ConfigurationPathUtils.ExampleParameterElementConfigPath;
    private readonly ConfigurationFileChangeSource _changeSource = new(
        [_configPath, _debugConfigPath, _exampleConfigPath]);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics()
    {
        List<(DiagnosticRule Rule, bool IsExample, bool IsDebug, string ConfigurationPath)> validRules = [];
        List<ParameterConfigurationError> errors = [];
        List<string> unreadableConfigurations = [];
        HashSet<string> codes = new(StringComparer.Ordinal);
        foreach ((string path, bool isExample, bool isDebug) in new[]
                 {
                     (_configPath, false, false), (_debugConfigPath, false, true), (_exampleConfigPath, true, false),
                 })
        {
            // Debug configurations are optional and user-owned: reading a missing file must not
            // create it, otherwise every machine grows an empty debug folder on startup.
            if (isDebug && !File.Exists(path)) continue;
            // A file with invalid YAML must not fail the whole catalog: it is skipped and
            // described through the configuration error state instead.
            if (!ConfigurationPathUtils.TryGetVersionedConfigurations(
                    path, out ConfigurationFile<DiagnosticRule>? file, out Exception? parseError))
            {
                logger.LogError(
                    parseError,
                    "Failed to parse parameter diagnostic configuration {ConfigurationPath}; treating it as empty",
                    path);
                unreadableConfigurations.Add(path);
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
            foreach (DiagnosticRule? rule in file?.Rules ?? [])
            {
                // An empty list item deserializes to null; there is nothing to register or to describe.
                if (rule is null || !codes.Add(rule.Code)) continue;

                IReadOnlyList<ParameterConfigurationError> ruleErrors = ParameterRuleValidator.Validate(
                    rule, ParameterIdentifierParser.IsKnownCategory, ParameterIdentifierParser.IsKnownGroup);
                if (ruleErrors.Count == 0)
                    validRules.Add((rule, isExample, isDebug, path));
                else
                    errors.AddRange(ruleErrors);
            }
        }

        ReportSkippedRules(errors, unreadableConfigurations);

        foreach ((DiagnosticRule rule, bool isExample, bool isDebug, string configurationPath) in validRules)
        {
            DocumentDiagnosticId identity = new(
                rule.Code, rule.Description,
                rule.Message ?? ParameterElementDiagnosticLocalizations.GetString("Default_message"),
                rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription,
                systemTags: DiagnosticTags.ForConfiguredRule(DiagnosticTags.ParameterProject, isExample, isTour: false, isDebug),
                tags: rule.Tags);
            yield return new DocumentDiagnosticRegistration(
                identity,
                new DocumentDiagnostic(documentQueries) { Identity = identity, Parameters = rule.Parameters },
                new DocumentDiagnosticFilter(documentFilterFactory) { Identity = identity, Formula = rule.Take },
                new DocumentDiagnosticIdOverride(identity, overrideStore),
                [],
                configurationPath)
            {
                Documentation = new("Project parameter diagnostics", "Проверки параметров проекта"),
            };
        }
    }

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => [];

    private void ReportSkippedRules(
        List<ParameterConfigurationError> errors, IReadOnlyList<string> unreadableConfigurations)
    {
        string? description = errors.Count == 0
            ? null
            : ParameterConfigurationErrorFormatter.Format(ConfigurationPathUtils.ParameterElementConfigFileName, errors);
        if (unreadableConfigurations.Count != 0)
        {
            string unreadable =
                $"Unreadable configuration file(s), treated as empty: {string.Join(", ", unreadableConfigurations)}.";
            description = description is null ? unreadable : description + Environment.NewLine + unreadable;
        }

        // The state reports a change only once per distinct description, which keeps the log and the
        // notification from repeating on every catalog refresh.
        if (configurationErrorState.Set(description) && description is not null)
            logger.LogWarning(
                "Skipped {RuleCount} invalid rule(s) in parameter diagnostic configuration {ConfigurationPath}: {Errors}",
                errors.Select(error => error.RuleCode).Distinct().Count(),
                _configPath,
                description);
    }
}
