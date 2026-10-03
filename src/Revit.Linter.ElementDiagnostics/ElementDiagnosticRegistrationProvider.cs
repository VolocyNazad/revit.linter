using Microsoft.Extensions.DependencyInjection;
using Toolkit.ValueStore.Abstractions;
using System.Reflection;

namespace Revit.Linter.ElementDiagnostics;

internal sealed class ElementDiagnosticRegistrationProvider(
    IServiceProvider serviceProvider,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
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
        IElementVisualizationPipeline[] visualizationPipelines =
            CreateImplementations<IElementVisualizationPipeline>(assembly, namespacePrefix);

        foreach (IElementDiagnostic diagnostic in diagnostics)
        {
            ElementDiagnosticId identity = diagnostic.Identity;
            IElementVisualizationPipeline[] diagnosticVisualizationPipelines = visualizationPipelines
                .Where(pipeline => string.Equals(
                    pipeline.Identity.Code, identity.Code, StringComparison.Ordinal))
                .ToArray();
            if (diagnosticVisualizationPipelines.Length == 0)
                diagnosticVisualizationPipelines =
                    [visualizationPipelineFactory.Create(
                        identity, "Show", [new ElementVisualizationStepDefinition { Type = "Show" }])];

            yield return new ElementDiagnosticRegistration(
                identity,
                diagnostic,
                FindByCode(filters, identity.Code, item => item.Identity.Code),
                FindByCode(documentFilters, identity.Code, item => item.Identity.Code),
                new ElementDiagnosticIdOverride(identity, overrideStore),
                fixes.Where(fix => string.Equals(
                    fix.Identity.Code, identity.Code, StringComparison.Ordinal)).ToArray(),
                diagnosticVisualizationPipelines)
            {
                Documentation = new("Built-in element diagnostics", "Встроенные проверки элементов"),
            };
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

    public IEnumerable<DocumentDiagnosticRegistration> GetDocumentDiagnostics() => [];
}
