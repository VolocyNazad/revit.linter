using Revit.TransactionMemoryCache.Abstractions.Services;
using Toolkit.Revit.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;

internal sealed class ParameterElementUnusedDiagnostic(
        IRevitTransactionMemoryCache revitTransactionMemoryCache) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ParameterElementUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        ParameterElement parameterElement = (ParameterElement)targetElement;
        Definition definition = parameterElement.GetDefinition();
        IList<Element>? elements = revitTransactionMemoryCache
            .GetOrCreate($"elements:document:{document.Title}\"", () =>
              new FilteredElementCollector(document).WherePasses(
                  ElementFilterUtils.AllFilter()).ToElements())
            ?? throw new InvalidOperationException($"Failed to get object from cache.");


        return elements.Any(i => i.get_Parameter(definition) is not null) 
            ? new(DiagnosticVerdict.Valid) 
            : new(DiagnosticVerdict.NotValid);
    }
}
