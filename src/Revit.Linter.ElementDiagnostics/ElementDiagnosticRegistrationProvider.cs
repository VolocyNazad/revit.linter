using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Revit.Linter.ElementVisualization;

namespace Revit.Linter.ElementDiagnostics;

internal sealed class ElementDiagnosticRegistrationProvider(
    IServiceProvider serviceProvider,
    IElementVisualizationPipelineFactory visualizationPipelineFactory,
    IBuiltInDiagnosticStateProvider builtInStateProvider)
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
                        identity,
                        ElementDiagnosticLocalizations.GetString("show_and_select_visualization"),
                        [new ElementVisualizationStepDefinition { Type = "Show" },
                            new ElementVisualizationStepDefinition { Type = "Select" }])];

            // Every built-in diagnostic carries the target-view pipeline, although only findings whose
            // target is a view offer it: the presenter decides per finding, so a check that starts
            // reporting views later needs no visualization of its own.
            IElementVisualizationPipeline openViewPipeline = new OpenViewVisualizationPipeline(
                identity,
                ElementDiagnosticLocalizations.GetString("open_view_visualization"),
                serviceProvider.GetRequiredService<ILogger<OpenViewVisualizationPipeline>>());
            diagnosticVisualizationPipelines = [.. diagnosticVisualizationPipelines, openViewPipeline];

            yield return new ElementDiagnosticRegistration(
                identity,
                diagnostic,
                FindByCode(filters, identity.Code, item => item.Identity.Code),
                FindByCode(documentFilters, identity.Code, item => item.Identity.Code),
                builtInStateProvider.GetState(identity.Code, identity.DefaultSeverity, identity.IsActive),
                fixes.Where(fix => string.Equals(
                    fix.Identity.Code, identity.Code, StringComparison.Ordinal)).ToArray(),
                diagnosticVisualizationPipelines,
                builtInStateProvider.ConfigurationPath)
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
