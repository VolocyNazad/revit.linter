using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Infrastructure.Extensions;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ViewHasHiddenElements;

internal sealed class ViewHasHiddenElementsDiagnostic(IDocumentQueryService documentQueries) : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ViewHasHiddenElements;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        if (targetElement is not View targetView) return DiagnosticFeedback.Valid;

        // Every element of the document is a candidate: an individually hidden element is absent
        // from the view-scoped collector, so the shared document list is the only place to find it.
        List<Element> hidden = HiddenInViewElements.Collect(documentQueries.GetElements(document), targetView);
        if (hidden.Count == 0) return DiagnosticFeedback.Valid;

        // Ordered by identifier so the message, the dependency list and the fix context are the same
        // on every run; the report renders every identifier as a clickable element link.
        hidden.Sort((first, second) => first.Id.IdValue().CompareTo(second.Id.IdValue()));
        return new DiagnosticFeedback(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["hidden.count"] = hidden.Count,
                ["hidden.elementNames"] = string.Join(", ", hidden.Select(element => element.Name)),
                ["hidden.elementIds"] = hidden.Select(element => element.Id).ToArray(),
            },
            [.. hidden]);
    }
}
