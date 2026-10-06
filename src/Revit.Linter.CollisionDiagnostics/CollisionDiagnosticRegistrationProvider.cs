using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
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
    IDocumentQueryService documentQueries,
    ILoggerFactory loggerFactory,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory, "collision.config.yaml");
    private static readonly string _exampleConfigPath = Path.Combine(
        ConfigurationPathUtils.Directory, "examples", "collision.config.yaml");
    private readonly ConfigurationFileChangeSource _changeSource = new([_configPath, _exampleConfigPath]);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        HashSet<string> codes = new(StringComparer.Ordinal);
        foreach ((string path, bool isExample) in new[] { (_configPath, false), (_exampleConfigPath, true) })
        {
            List<DiagnosticRule>? rules = ConfigurationPathUtils.GetConfigurations<List<DiagnosticRule>>(path);
            foreach (DiagnosticRule rule in rules ?? [])
            {
                if (!codes.Add(rule.Code)) continue;
                ElementDiagnosticId identity = new(
                    rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                    rule.IsObsolete, rule.ObsoleteDescription, isExample);
                yield return new ElementDiagnosticRegistration(
                    identity,
                    new ElementDiagnostic(
                        elementFilterFactory, elementFunctionFactory, boundingBoxService, geometryService,
                        documentQueries, loggerFactory.CreateLogger<ElementDiagnostic>())
                        { Identity = identity, TakeFormula = rule.AndTake, GroupByFormula = rule.GroupBy },
                    new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                    new ElementDiagnosticDocumentFilter(documentFilterFactory)
                        { Identity = identity, Formula = rule.TakeDocument },
                    new ElementDiagnosticIdOverride(identity, overrideStore),
                    [],
                    rule.Visualizations
                        .Select(pipeline => visualizationPipelineFactory.Create(
                            identity, pipeline.Name, pipeline.Steps))
                        .ToArray(),
                    path)
            {
                Documentation = new("Collision diagnostics", "Проверки коллизий"),
            };
            }
        }
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];
}
