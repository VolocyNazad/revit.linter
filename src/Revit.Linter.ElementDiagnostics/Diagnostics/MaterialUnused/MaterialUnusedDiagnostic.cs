using Revit.Linter.DocumentQueries.Abstractions.Services;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

internal sealed class MaterialUnusedDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.MaterialUnused;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        var material = (Material)targetElement;
        IReadOnlyCollection<ElementId> usedMaterialIds = UsedMaterialIds.Get(documentQueries, document);
        return usedMaterialIds.Contains(material.Id)
            ? new(DiagnosticVerdict.Valid)
            : new(DiagnosticVerdict.NotValid);
    }
}
