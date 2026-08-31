using Microsoft.Extensions.Logging;
using Revit.Linter.CollisionDiagnostics.Abstractions.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.Models;
using Revit.Linter.ConfigurationPath;
using Toolkit.ValueStore.Abstractions;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.CollisionDiagnostics;

internal sealed class CollisionDiagnosticRegistrationProvider(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    DocumentFilterFactory documentFilterFactory,
    IGetElementBoundingBoxService boundingBoxService,
    IGetElementGeometryService geometryService,
    IRevitTransactionMemoryCache transactionMemoryCache,
    ILoggerFactory loggerFactory,
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory, "collision.config.yaml");
    private readonly ConfigurationFileChangeSource _changeSource = new(_configPath);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        List<DiagnosticRule>? rules = ConfigurationPathUtils.GetConfigurations<List<DiagnosticRule>>(_configPath);
        if (rules is null) yield break;

        foreach (DiagnosticRule rule in rules)
        {
            ElementDiagnosticId identity = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription);
            yield return new ElementDiagnosticRegistration(
                identity,
                new ElementDiagnostic(
                    elementFilterFactory, elementFunctionFactory, boundingBoxService, geometryService,
                    transactionMemoryCache, loggerFactory.CreateLogger<ElementDiagnostic>())
                    { Identity = identity, TakeFormula = rule.AndTake, GroupByFormula = rule.GroupBy },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new ElementDiagnosticIdOverride(identity, overrideStore),
                []);
        }
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];
}