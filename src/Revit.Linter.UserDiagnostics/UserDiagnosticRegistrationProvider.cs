using Revit.Linter.Languages.Factories;
using Microsoft.Extensions.Logging;
using UserDiagnosticRule = Revit.Linter.UserDiagnostics.Models.DiagnosticRule;

namespace Revit.Linter.UserDiagnostics;

internal sealed class UserDiagnosticRegistrationProvider(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    DocumentFilterFactory documentFilterFactory,
    IElementFixPipelineFactory fixPipelineFactory,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
    IDiagnosticConfigurationService configurationService,
    IDiagnosticConfigurationErrorState configurationErrorState,
    ILogger<UserDiagnosticRegistrationProvider> logger)
    : IDiagnosticRegistrationProvider
{
    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        List<ElementDiagnosticRegistration> registrations = [];
        List<string> errors = [];
        HashSet<string> codes = new(StringComparer.Ordinal);

        foreach (DiagnosticRuleSource source in configurationService.GetRules())
        {
            if (!DiagnosticRuleTypes.TryParse(source.Rule.Type, out DiagnosticRuleType type)
                || type != DiagnosticRuleType.Custom)
                continue;

            UserDiagnosticRule? rule = ToRule(source.Rule);
            if (rule is null || !codes.Add(rule.Code))
                continue;
            ElementDiagnosticRegistration? registration = TryCreateRegistration(rule, source, errors);
            if (registration is not null)
                registrations.Add(registration);
        }

        if (errors.Count > 0)
            configurationErrorState.AddRuleErrors(errors);

        return registrations;
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];

    private ElementDiagnosticRegistration? TryCreateRegistration(
        UserDiagnosticRule rule,
        DiagnosticRuleSource source,
        List<string> errors)
    {
        try
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description,
                rule.Message ?? UserDiagnosticLocalizations.GetString("Default_message"),
                rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription,
                systemTags: DiagnosticTags.ForConfiguredRule(
                    DiagnosticTags.Custom, source.IsExample, source.IsTour, source.IsDebug),
                tags: rule.Tags);
            return new ElementDiagnosticRegistration(
                identity,
                new ElementDiagnostic(elementFunctionFactory) { Identity = identity, Formula = rule.Check },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new DiagnosticState(rule.IsActive, rule.Severity, isHidden: source.Rule.IsHidden ?? false),
                CreateFixes(rule, (name, steps) => fixPipelineFactory.Create(identity, name, steps)),
                rule.Visualizations
                    .Select(pipeline => visualizationPipelineFactory.Create(
                        identity, pipeline.Name, pipeline.Steps))
                    .ToArray(),
                source.Path)
            {
                RuleType = DiagnosticRuleType.Custom,
                Documentation = new("User diagnostics", "Пользовательские проверки"),
            };
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Skipping user rule {RuleCode} from {ConfigurationPath}: its fix or visualization pipelines are invalid",
                rule.Code, source.Path);
            errors.Add($"diagnostics.config.yaml: rule '{rule.Code}' was skipped: its fix or visualization pipeline is invalid");
            return null;
        }
    }

    private static T[] CreateFixes<T>(
        UserDiagnosticRule rule,
        Func<string, IReadOnlyList<ElementFixStepDefinition>, T> factory) => rule.Fixes
        .Select(pipeline => factory(pipeline.Name, pipeline.Steps))
        .ToArray();

    private static UserDiagnosticRule? ToRule(DiagnosticRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Code)
            || string.IsNullOrWhiteSpace(rule.TakeDocument)
            || string.IsNullOrWhiteSpace(rule.Take)
            || string.IsNullOrWhiteSpace(rule.Check))
            return null;

        return new UserDiagnosticRule
        {
            Code = rule.Code ?? string.Empty,
            TakeDocument = rule.TakeDocument ?? string.Empty,
            Take = rule.Take ?? string.Empty,
            Check = rule.Check ?? string.Empty,
            Description = rule.Description ?? string.Empty,
            Message = rule.Message,
            Severity = rule.Severity ?? DiagnosticSeverity.Message,
            IsActive = rule.IsActive ?? true,
            IsObsolete = rule.IsObsolete ?? false,
            ObsoleteDescription = rule.ObsoleteDescription ?? string.Empty,
            Tags = rule.Tags,
            Fixes = rule.Fixes,
            Visualizations = rule.Visualizations.Length > 0
                ? rule.Visualizations
                : [new() { Name = "Show and select", Steps = [new() { Type = "Show" }, new() { Type = "Select" }] }],
        };
    }
}

