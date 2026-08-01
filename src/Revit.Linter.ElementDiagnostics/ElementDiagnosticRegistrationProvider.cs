using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ValueStore.Abstractions.Services;
using System.Reflection;

namespace Revit.Linter.ElementDiagnostics;

internal sealed class ElementDiagnosticRegistrationProvider(
    IServiceProvider serviceProvider,
    IValueStore<ElementDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider
{
    public IEnumerable<ElementDiagnosticRegistration> GetElementDiagnostics()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string namespacePrefix = typeof(ElementDiagnosticIdCollector).Namespace! + ".";

        IElementDiagnostic[] diagnostics = CreateImplementations<IElementDiagnostic>(assembly, namespacePrefix);
        IElementDiagnosticFilter[] filters = CreateImplementations<IElementDiagnosticFilter>(assembly, namespacePrefix);
        IElementDiagnosticDocumentFilter[] documentFilters =
            CreateImplementations<IElementDiagnosticDocumentFilter>(assembly, namespacePrefix);
        IElementFix[] fixes = CreateImplementations<IElementFix>(assembly, namespacePrefix);

        foreach (IElementDiagnostic diagnostic in diagnostics)
        {
            ElementDiagnosticId identity = diagnostic.Identity;
            yield return new ElementDiagnosticRegistration(
                identity,
                diagnostic,
                FindByCode(filters, identity.Code, item => item.Identity.Code),
                FindByCode(documentFilters, identity.Code, item => item.Identity.Code),
                new ElementDiagnosticIdOverride(identity, overrideStore),
                fixes.Where(fix => string.Equals(
                    fix.Identity.Code, identity.Code, StringComparison.Ordinal)).ToArray());
        }
    }

    private T[] CreateImplementations<T>(Assembly assembly, string namespacePrefix) => assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract &&
                       type.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true &&
                       typeof(T).IsAssignableFrom(type))
        .Select(type => (T)ActivatorUtilities.CreateInstance(serviceProvider, type))
        .ToArray();

    private static T FindByCode<T>(IEnumerable<T> items, string code, Func<T, string> getCode) =>
        items.Single(item => string.Equals(getCode(item), code, StringComparison.Ordinal));
}
