using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;

internal sealed class PurgeableElementUnusedDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string UnusedElementIdsQuery = "element-diagnostics:purgeable-unused-ids";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.PurgeableElementUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        IReadOnlyCollection<ElementId> unusedIds = documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, UnusedElementIdsQuery),
            () => PurgeableElementIds.GetIds(document));
        return unusedIds.Contains(targetElement.Id)
            ? new(DiagnosticVerdict.NotValid)
            : new(DiagnosticVerdict.Valid);
    }
}
