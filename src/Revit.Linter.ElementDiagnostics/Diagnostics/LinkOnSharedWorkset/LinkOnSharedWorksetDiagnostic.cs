using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.LinkOnSharedWorkset;

internal sealed class LinkOnSharedWorksetDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.LinkOnSharedWorkset;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        if (!LinkElement.IsLink(targetElement)) return DiagnosticFeedback.Valid;
        if (!document.IsWorkshared) return DiagnosticFeedback.Valid;

        // An unreadable workset leaves the rule with nothing to compare, so the link counts as
        // clean instead of failing the whole run.
        if (TryGetWorkset(targetElement, document) is not { } workset) return DiagnosticFeedback.Valid;

        IReadOnlyList<Element> occupants = LinkWorksetOccupants.Get(documentQueries, document, workset.Id);
        if (occupants.Count == 0) return DiagnosticFeedback.Valid;

        return new DiagnosticFeedback(
            DiagnosticVerdict.NotValid,
            new Dictionary<string, object>
            {
                ["workset.name"] = workset.Name,
                ["other.count"] = occupants.Count,
                ["other.elementNames"] = occupants.Select(element => element.Name).ToArray(),
                ["other.elementIds"] = occupants.Select(element => element.Id).ToArray(),
            },
            [.. occupants]);
    }

    private static Workset? TryGetWorkset(Element element, Document document)
    {
        try
        {
            return document.GetWorksetTable().GetWorkset(element.WorksetId);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
