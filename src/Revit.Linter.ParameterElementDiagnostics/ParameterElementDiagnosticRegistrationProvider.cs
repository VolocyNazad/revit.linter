using Microsoft.Extensions.Logging;
using Revit.Linter.ConfigurationPath;
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
    private const string ConfigFileName = "parameter-element.config.yaml";
    private static readonly string _configPath = Path.Combine(ConfigurationPathUtils.Directory, ConfigFileName);
    private readonly ConfigurationFileChangeSource _changeSource = new(_configPath);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics()
    {
        List<DiagnosticRule>? rules = ConfigurationPathUtils.GetConfigurations<List<DiagnosticRule>>(_configPath);
        List<DiagnosticRule> validRules = [];
        List<ParameterConfigurationError> errors = [];
        foreach (DiagnosticRule? rule in rules ?? [])
        {
            // An empty list item deserializes to null; there is nothing to register or to describe.
            if (rule is null) continue;

            IReadOnlyList<ParameterConfigurationError> ruleErrors = ParameterRuleValidator.Validate(
                rule, ParameterIdentifierParser.IsKnownCategory, ParameterIdentifierParser.IsKnownGroup);
            if (ruleErrors.Count == 0)
                validRules.Add(rule);
            else
                errors.AddRange(ruleErrors);
        }

        ReportSkippedRules(errors);

        foreach (DiagnosticRule rule in validRules)
        {
            DocumentDiagnosticId identity = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription);
            yield return new DocumentDiagnosticRegistration(
                identity,
                new DocumentDiagnostic(documentQueries) { Identity = identity, Parameters = rule.Parameters },
                new DocumentDiagnosticFilter(documentFilterFactory) { Identity = identity, Formula = rule.Take },
                new DocumentDiagnosticIdOverride(identity, overrideStore),
                [])
            {
                Documentation = new("Project parameter diagnostics", "Проверки параметров проекта"),
            };
        }
    }

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => [];

    private void ReportSkippedRules(List<ParameterConfigurationError> errors)
    {
        string? description = errors.Count == 0
            ? null
            : ParameterConfigurationErrorFormatter.Format(ConfigFileName, errors);

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