using Revit.Linter.ConfigurationPath;
using Revit.Linter.UserDiagnostics.Models;
using Toolkit.ValueStore.Abstractions;

namespace Revit.Linter.UserDiagnostics;

internal sealed class UserDiagnosticRegistrationProvider(
    ElementFilterFactory elementFilterFactory,
    ElementFunctionFactory elementFunctionFactory,
    DocumentFilterFactory documentFilterFactory,
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider, IDiagnosticCatalogChangeSource, IDisposable
{
    private static readonly string _configPath = Path.Combine(ConfigurationPathUtils.Directory, "config.yaml");
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
                new ElementDiagnostic(elementFunctionFactory) { Identity = identity, Formula = rule.Check },
                new ElementDiagnosticFilter(elementFilterFactory) { Identity = identity, Formula = rule.Take },
                new ElementDiagnosticDocumentFilter(documentFilterFactory)
                    { Identity = identity, Formula = rule.TakeDocument },
                new ElementDiagnosticIdOverride(identity, overrideStore),
                []);
        }
    }

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];
}