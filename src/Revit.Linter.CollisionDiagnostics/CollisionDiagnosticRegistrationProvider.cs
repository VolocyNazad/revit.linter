using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using CollisionDiagnosticRule = Revit.Linter.CollisionDiagnostics.Models.DiagnosticRule;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class CollisionDiagnosticRegistrationProvider(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    DocumentFilterFactory documentFilterFactory,
    IGetElementBoundingBoxService boundingBoxService,
    IGetElementGeometryService geometryService,
    IGetConnectedElementIdsService connectedElementIdsService,
    IDocumentQueryService documentQueries,
    ILoggerFactory loggerFactory,
    IElementFixPipelineFactory fixPipelineFactory,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
    IDiagnosticConfigurationService configurationService,
    IDiagnosticConfigurationErrorState configurationErrorState)
    : IDiagnosticRegistrationProvider
{
    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger =
            loggerFactory.CreateLogger<CollisionDiagnosticRegistrationProvider>();
        List<ElementDiagnosticRegistration> registrations = [];
        List<string> errors = [];
        HashSet<string> codes = new(StringComparer.Ordinal);

        foreach (DiagnosticRuleSource source in configurationService.GetRules())
        {
            if (!DiagnosticRuleTypes.TryParse(source.Rule.Type, out DiagnosticRuleType type)
                || type != DiagnosticRuleType.Collision)
                continue;

            CollisionDiagnosticRule? rule = ToRule(source.Rule);
            if (rule is null || !codes.Add(rule.Code))
                continue;

            ElementDiagnosticRegistration? registration =
                TryCreateRegistration(rule, source, providerLogger, errors);
            if (registration is not null)
                registrations.Add(registration);
        }

        if (errors.Count > 0)
            configurationErrorState.AddRuleErrors(errors);

        return registrations;
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];

    private ElementDiagnosticRegistration? TryCreateRegistration(
        CollisionDiagnosticRule rule,
        DiagnosticRuleSource source,
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger,
        List<string> errors)
    {
        try
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description,
                rule.Message ?? CollisionDiagnosticLocalizations.GetString("Default_message"),
                rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription,
                systemTags: DiagnosticTags.ForConfiguredRule(
                    DiagnosticTags.Collision, source.IsExample, source.IsTour, source.IsDebug),
                tags: rule.Tags);
            return new ElementDiagnosticRegistration(
                identity,
                new ElementDiagnostic(
                    elementFilterFactory, elementFunctionFactory, boundingBoxService, geometryService,
                    connectedElementIdsService, documentQueries, loggerFactory.CreateLogger<ElementDiagnostic>())
                    {
                        Identity = identity,
                        TakeFormula = rule.AndTake,
                        GroupByFormula = rule.GroupBy,
                        IncludeLinks = rule.IncludeLinks,
                        IgnoreConnectedElements = rule.IgnoreConnectedElements,
                        IgnoreNestedFamilies = rule.IgnoreNestedFamilies,
                    },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new DiagnosticState(rule.IsActive, rule.Severity, source.Rule.IsHidden ?? false),
                CreateFixes(rule, (name, steps) => fixPipelineFactory.Create(identity, name, steps)),
                rule.Visualizations
                    .Select(pipeline => visualizationPipelineFactory.Create(
                        identity, pipeline.Name, pipeline.Steps))
                    .ToArray(),
                source.Path)
            {
                RuleType = DiagnosticRuleType.Collision,
                Documentation = new("Collision diagnostics", "Проверки коллизий"),
            };
        }
        catch (Exception exception)
        {
            providerLogger.LogError(
                exception,
                "Skipping collision rule {RuleCode} from {ConfigurationPath}: its pipelines are invalid",
                rule.Code, source.Path);
            errors.Add($"diagnostics.config.yaml: rule '{rule.Code}' was skipped: its fix or visualization pipeline is invalid");
            return null;
        }
    }

    private static T[] CreateFixes<T>(
        CollisionDiagnosticRule rule,
        Func<string, IReadOnlyList<ElementFixStepDefinition>, T> factory) => rule.Fixes
        .Select(pipeline => factory(pipeline.Name, pipeline.Steps))
        .ToArray();

    private static CollisionDiagnosticRule? ToRule(DiagnosticRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Code)
            || string.IsNullOrWhiteSpace(rule.TakeDocument)
            || string.IsNullOrWhiteSpace(rule.Take)
            || string.IsNullOrWhiteSpace(rule.AndTake)
            || string.IsNullOrWhiteSpace(rule.GroupBy))
            return null;

        return new CollisionDiagnosticRule
        {
            Code = rule.Code ?? string.Empty,
            TakeDocument = rule.TakeDocument ?? string.Empty,
            Take = rule.Take ?? string.Empty,
            AndTake = rule.AndTake ?? string.Empty,
            GroupBy = rule.GroupBy ?? string.Empty,
            IncludeLinks = rule.IncludeLinks ?? false,
            IgnoreConnectedElements = rule.IgnoreConnectedElements ?? false,
            IgnoreNestedFamilies = rule.IgnoreNestedFamilies ?? false,
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

