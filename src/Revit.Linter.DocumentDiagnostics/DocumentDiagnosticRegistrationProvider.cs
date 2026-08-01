using Microsoft.Extensions.DependencyInjection;
using Revit.Linter.ValueStore.Abstractions.Services;
using System.Reflection;

namespace Revit.Linter.DocumentDiagnostics;

internal sealed class DocumentDiagnosticRegistrationProvider(
    IServiceProvider serviceProvider,
    IValueStore<DocumentDiagnosticOverridesSettings> overrideStore)
    : IDiagnosticRegistrationProvider
{
    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string namespacePrefix = typeof(DocumentDiagnosticIdCollector).Namespace! + ".";

        IDocumentDiagnostic[] diagnostics = CreateImplementations<IDocumentDiagnostic>(assembly, namespacePrefix);
        IDocumentDiagnosticFilter[] filters = CreateImplementations<IDocumentDiagnosticFilter>(assembly, namespacePrefix);

        foreach (IDocumentDiagnostic diagnostic in diagnostics)
        {
            DocumentDiagnosticId identity = diagnostic.Identity;
            yield return new DocumentDiagnosticRegistration(
                identity,
                diagnostic,
                filters.Single(filter => string.Equals(
                    filter.Identity.Code, identity.Code, StringComparison.Ordinal)),
                new DocumentDiagnosticIdOverride(identity, overrideStore),
                []);
        }
    }

    private T[] CreateImplementations<T>(Assembly assembly, string namespacePrefix) => assembly.GetTypes()
        .Where(type => type.IsClass && !type.IsAbstract &&
                       type.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true &&
                       typeof(T).IsAssignableFrom(type))
        .Select(type => (T)ActivatorUtilities.CreateInstance(serviceProvider, type))
        .ToArray();
}
