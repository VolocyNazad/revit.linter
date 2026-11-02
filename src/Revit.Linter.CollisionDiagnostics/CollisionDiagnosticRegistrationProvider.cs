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

            string? formulaError = FindFormulaError(rule);
            if (formulaError is not null)
            {
                providerLogger.LogWarning(
                    "Skipping collision rule {RuleCode} from {ConfigurationPath}: {FormulaError}",
                    rule.Code, source.Path, formulaError);
                errors.Add($"{Path.GetFileName(source.Path)}: rule '{rule.Code}' was skipped: {formulaError}");
                continue;
            }

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

    private string? FindFormulaError(CollisionDiagnosticRule rule)
    {
        if (!documentFilterFactory.TryValidateBoolean(rule.TakeDocument))
            return DescribeFormulaError("takeDocument", rule.TakeDocument);
        if (!elementFilterFactory.TryValidate(rule.Take))
            return DescribeFormulaError("take", rule.Take);
        if (!elementFilterFactory.TryValidate(rule.AndTake))
            return DescribeFormulaError("andTake", rule.AndTake);
        if (!elementFunctionFactory.TryValidate<object>(rule.GroupBy))
            return DescribeFormulaError("groupBy", rule.GroupBy);

        return null;
    }

    private static string DescribeFormulaError(string field, string formula) =>
        $"the {field} formula cannot be compiled: {formula}";

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
                CreateFixes(rule, identity, source, errors, providerLogger),
                CreateVisualizations(rule, identity, source, errors, providerLogger),
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
                "Skipping collision rule {RuleCode} from {ConfigurationPath}: the rule cannot be registered",
                rule.Code, source.Path);
            errors.Add(
                $"{Path.GetFileName(source.Path)}: rule '{rule.Code}' was skipped: the rule cannot be registered");
            return null;
        }
    }

    private IElementFix[] CreateFixes(
        CollisionDiagnosticRule rule,
        ElementDiagnosticId identity,
        DiagnosticRuleSource source,
        List<string> errors,
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger)
    {
        List<IElementFix> fixes = [];
        foreach (ElementFixPipelineDefinition pipeline in rule.Fixes)
        {
            try
            {
                fixes.Add(fixPipelineFactory.Create(identity, pipeline.Name, pipeline.Steps));
            }
            catch (Exception exception)
            {
                providerLogger.LogWarning(
                    exception,
                    "Skipping fix {FixName} of collision rule {RuleCode} from {ConfigurationPath}: {Reason}",
                    pipeline.Name, rule.Code, source.Path, exception.Message);
                errors.Add(
                    $"{Path.GetFileName(source.Path)}: rule '{rule.Code}': fix '{pipeline.Name}' was skipped: " +
                    exception.Message);
            }
        }

        return fixes.ToArray();
    }

    private IElementVisualizationPipeline[] CreateVisualizations(
        CollisionDiagnosticRule rule,
        ElementDiagnosticId identity,
        DiagnosticRuleSource source,
        List<string> errors,
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger)
    {
        List<IElementVisualizationPipeline> visualizations = [];
        foreach (ElementVisualizationPipelineDefinition pipeline in rule.Visualizations)
        {
            try
            {
                visualizations.Add(
                    visualizationPipelineFactory.Create(identity, pipeline.Name, pipeline.Steps));
            }
            catch (Exception exception)
            {
                providerLogger.LogWarning(
                    exception,
                    "Skipping visualization {VisualizationName} of collision rule {RuleCode} from {ConfigurationPath}: {Reason}",
                    pipeline.Name, rule.Code, source.Path, exception.Message);
                errors.Add(
                    $"{Path.GetFileName(source.Path)}: rule '{rule.Code}': visualization '{pipeline.Name}' was skipped: " +
                    exception.Message);
            }
        }

        return visualizations.ToArray();
    }

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