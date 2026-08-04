using Revit.Linter.ConfigurationPath;
using Revit.Linter.ParameterElementDiagnostics.Models;
using Revit.Linter.ValueStore.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ParameterElementDiagnostics;

internal sealed class ParameterElementDiagnosticRegistrationProvider(
    DocumentFilterFactory documentFilterFactory,
    IRevitTransactionMemoryCache transactionMemoryCache,
    IValueStore<DocumentDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = Path.Combine(
        ConfigurationPathUtils.Directory, "parameter-element.config.yaml");
    private readonly ConfigurationFileChangeSource _changeSource = new(_configPath);

    public IDisposable OnChange(Action listener) => _changeSource.OnChange(listener);
    public void Dispose() => _changeSource.Dispose();

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics()
    {
        List<DiagnosticRule>? rules = ConfigurationPathUtils.GetConfigurations<List<DiagnosticRule>>(_configPath);
        if (rules is null) yield break;

        foreach (DiagnosticRule rule in rules)
        {
            DocumentDiagnosticId identity = new(
                rule.Code, rule.Description, rule.Message, rule.Severity, rule.IsActive,
                rule.IsObsolete, rule.ObsoleteDescription);
            yield return new DocumentDiagnosticRegistration(
                identity,
                new DocumentDiagnostic(transactionMemoryCache) { Identity = identity, Parameters = rule.Parameters },
                new DocumentDiagnosticFilter(documentFilterFactory) { Identity = identity, Formula = rule.Take },
                new DocumentDiagnosticIdOverride(identity, overrideStore),
                []);
        }
    }

    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics() => [];
}
