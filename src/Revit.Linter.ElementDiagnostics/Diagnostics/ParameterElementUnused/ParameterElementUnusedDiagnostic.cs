using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.TransactionMemoryCache.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ParameterElementUnused;

internal sealed class ParameterElementUnusedDiagnostic(
        IRevitTransactionMemoryCache revitTransactionMemoryCache) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ParameterElementUnused;

    public DiagnosticResult Execute(Document document, View? view, Element targetElement)
    {
        ParameterElement parameterElement = (ParameterElement)targetElement;
        Definition definition = parameterElement.GetDefinition();
        IList<Element>? elements = revitTransactionMemoryCache
            .GetOrCreate($"elements:document:{document.Title}\"", () =>
              new FilteredElementCollector(document).WherePasses(
                  new LogicalOrFilter(
                    new ElementIsElementTypeFilter(false),
                    new ElementIsElementTypeFilter(true))).ToElements())
            ?? throw new InvalidOperationException($"Failed to get object from cache.");
        foreach (var element in elements)
        {
            if (element.get_Parameter(definition) is not null) return new(DiagnosticVerdict.Valid);
        }
        return new(DiagnosticVerdict.NotValid);
    }
}
