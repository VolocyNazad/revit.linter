using Revit.Linter.ConfigurationPath;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.ParameterElementDiagnostics.Infrastructure.Utils;
using Revit.Linter.ParameterElementDiagnostics.Models;
using ParameterDiagnosticRule = Revit.Linter.ParameterElementDiagnostics.Models.DiagnosticRule;
using ParameterElementData = Revit.Linter.ParameterElementDiagnostics.Models.ParameterElementData;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class ParameterElementDiagnosticRegistrationProvider(
    DocumentFilterFactory documentFilterFactory,
    IDocumentQueryService documentQueries,
    IDiagnosticConfigurationService configurationService,
    IDiagnosticConfigurationErrorState configurationErrorState)
    : IDiagnosticRegistrationProvider
{
    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics()
    {
        List<DocumentDiagnosticRegistration> registrations = [];
        List<ParameterConfigurationError> errors = [];
        HashSet<string> codes = new(StringComparer.Ordinal);

        foreach (DiagnosticRuleSource source in configurationService.GetRules())
        {
            if (!DiagnosticRuleTypes.TryParse(source.Rule.Type, out DiagnosticRuleType type)
                || type != DiagnosticRuleType.Parameter)
                continue;

            ParameterDiagnosticRule? rule = ToRule(source.Rule);
            if (rule is null || !codes.Add(rule.Code))
                continue;

            IReadOnlyList<ParameterConfigurationError> ruleErrors = ParameterRuleValidator.Validate(
                rule, ParameterIdentifierParser.IsKnownCategory, ParameterIdentifierParser.IsKnownGroup);
            if (ruleErrors.Count > 0)
            {
                errors.AddRange(ruleErrors);
                continue;
            }

            registrations.Add(CreateRegistration(rule, source));
        }

        if (errors.Count > 0)
        {
            configurationErrorState.AddRuleErrors(
                [ParameterConfigurationErrorFormatter.Format(
                    ConfigurationPathUtils.DiagnosticsConfigFileName, errors)]);
        }

        return registrations;
    }

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => [];

    private DocumentDiagnosticRegistration CreateRegistration(
        ParameterDiagnosticRule rule,
        DiagnosticRuleSource source)
    {
        DocumentDiagnosticId identity = new(
            rule.Code, rule.Description,
            rule.Message ?? ParameterElementDiagnosticLocalizations.GetString("Default_message"),
            rule.Severity, rule.IsActive,
            rule.IsObsolete, rule.ObsoleteDescription,
            systemTags: DiagnosticTags.ForConfiguredRule(
                DiagnosticTags.ParameterProject, source.IsExample, source.IsTour, source.IsDebug),
            tags: rule.Tags);
        return new DocumentDiagnosticRegistration(
            identity,
            new DocumentDiagnostic(documentQueries) { Identity = identity, Parameters = rule.Parameters },
            new DocumentDiagnosticFilter(documentFilterFactory) { Identity = identity, Formula = rule.Take },
            new DiagnosticState(rule.IsActive, rule.Severity, source.Rule.IsHidden ?? false),
            [],
            source.Path)
        {
            RuleType = DiagnosticRuleType.Parameter,
            Documentation = new("Project parameter diagnostics", "Проверки параметров проекта"),
        };
    }

    private static ParameterDiagnosticRule? ToRule(Revit.Linter.Core.Abstractions.Models.DiagnosticRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Code) || string.IsNullOrWhiteSpace(rule.Take))
            return null;

        return new ParameterDiagnosticRule
        {
            Code = rule.Code ?? string.Empty,
            Description = rule.Description ?? string.Empty,
            Message = rule.Message,
            Severity = rule.Severity ?? DiagnosticSeverity.Message,
            IsActive = rule.IsActive ?? true,
            IsObsolete = rule.IsObsolete ?? false,
            ObsoleteDescription = rule.ObsoleteDescription ?? string.Empty,
            Tags = rule.Tags,
            Take = rule.Take ?? string.Empty,
            Parameters = rule.Parameters is null
                ? []
                : rule.Parameters.Select(parameter => new ParameterElementData
                {
                    Name = parameter.Name ?? string.Empty,
                    Guid = parameter.Guid ?? string.Empty,
                    IsInstance = parameter.IsInstance,
                    Categories = parameter.Categories ?? [],
                    AllowVaryBetweenGroups = parameter.AllowVaryBetweenGroups,
                    Group = parameter.Group ?? string.Empty,
                }).ToList(),
        };
    }
}

