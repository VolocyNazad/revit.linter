using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
using Revit.Linter.ConfigurationPath.Abstractions.Models;
using Toolkit.ValueStore.Abstractions;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;

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
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore,
    CollisionDiagnosticConfigurationErrorState configurationErrorState)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = ConfigurationPathUtils.CollisionConfigPath;
    private static readonly string _debugConfigPath = ConfigurationPathUtils.DebugCollisionConfigPath;
    private static readonly string _exampleConfigPath = ConfigurationPathUtils.ExampleCollisionConfigPath;
    private static readonly string _practicalTourConfigPath = ConfigurationPathUtils.PracticalTourConfigPath;
    private readonly ConfigurationFileChangeSource _changeSource = new(
        [_configPath, _debugConfigPath, _exampleConfigPath, _practicalTourConfigPath]);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        HashSet<string> codes = new(StringComparer.Ordinal);
        List<string> skippedRules = [];
        List<string> unreadableConfigurations = [];
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger =
            loggerFactory.CreateLogger<CollisionDiagnosticRegistrationProvider>();
        foreach ((string path, bool isExample, bool isTour, bool isDebug) in new[]
        {
            (_configPath, false, false, false),
            (_debugConfigPath, false, false, true),
            (_exampleConfigPath, true, false, false),
            (_practicalTourConfigPath, false, true, false),
        })
        {
            // The managed tour configuration is optional and owned by the practical tour:
            // reading it must not recreate the file after the tour removed it, otherwise
            // the recreated file triggers a second catalog refresh and a duplicate notification.
            // Debug configurations are optional and user-owned for the same reason.
            if ((isTour || isDebug) && !File.Exists(path)) continue;
            // A file with invalid YAML must not fail the whole catalog: it is skipped and
            // described through the configuration error state instead.
            if (!ConfigurationPathUtils.TryGetVersionedConfigurations(
                    path, out ConfigurationFile<DiagnosticRule>? file, out Exception? parseError))
            {
                providerLogger.LogError(
                    parseError,
                    "Failed to parse collision configuration {ConfigurationPath}; treating it as empty",
                    path);
                unreadableConfigurations.Add(path);
                continue;
            }
            if (file is not null && file.Version > ConfigurationPathUtils.SupportedConfigurationVersion)
            {
                providerLogger.LogError(
                    "Diagnostic configuration {ConfigurationPath} has version {ConfigurationVersion}, " +
                    "newer than supported version {SupportedVersion}; ignoring the file",
                    path, file.Version, ConfigurationPathUtils.SupportedConfigurationVersion);
                continue;
            }
            foreach (DiagnosticRule? rule in file?.Rules ?? [])
            {
                // An empty list item deserializes to null; there is nothing to register.
                if (rule is null || !codes.Add(rule.Code)) continue;
                ElementDiagnosticRegistration? registration = TryCreateRegistration(
                    rule, isExample, isTour, isDebug, path, providerLogger);
                if (registration is null)
                    skippedRules.Add($"rule '{rule.Code}' from {path}");
                else
                    yield return registration;
            }
        }

        List<string> descriptions = [];
        if (skippedRules.Count != 0)
            descriptions.Add($"Collision rules skipped: {string.Join(", ", skippedRules)}.");
        if (unreadableConfigurations.Count != 0)
            descriptions.Add(
                $"Collision configurations unreadable, treated as empty: {string.Join(", ", unreadableConfigurations)}.");
        configurationErrorState.Set(descriptions.Count == 0 ? null : string.Join(" ", descriptions));
    }

    // A rule with invalid pipelines must not fail the whole catalog: it is skipped and
    // described through the configuration error state instead.
    private ElementDiagnosticRegistration? TryCreateRegistration(
        DiagnosticRule rule,
        bool isExample,
        bool isTour,
        bool isDebug,
        string path,
        ILogger<CollisionDiagnosticRegistrationProvider> providerLogger)
    {
        try
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description,
                rule.Message ?? CollisionDiagnosticLocalizations.GetString("Default_message"),
                rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription,
                systemTags: DiagnosticTags.ForConfiguredRule(DiagnosticTags.Collision, isExample, isTour, isDebug),
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
                    },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new ElementDiagnosticIdOverride(identity, overrideStore),
                CreateFixes(rule, (name, steps) => fixPipelineFactory.Create(identity, name, steps)),
                rule.Visualizations
                    .Select(pipeline => visualizationPipelineFactory.Create(
                        identity, pipeline.Name, pipeline.Steps))
                    .ToArray(),
                path)
            {
                Documentation = new("Collision diagnostics", "Проверки коллизий"),
            };
        }
        catch (Exception exception)
        {
            providerLogger.LogError(
                exception,
                "Skipping collision rule {RuleCode} from {ConfigurationPath}: its pipelines are invalid",
                rule.Code, path);
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
