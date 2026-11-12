using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;

internal sealed class DeleteDuplicateMaterial(IDocumentQueryService documentQueries) : IElementFix
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateMaterial;

    public string Value => ElementDiagnosticLocalizations.GetString("deleteDuplicateMaterial_fix");

    public bool Execute(Element targetElement)
    {
        if (targetElement is not Material material) return false;
        Document document = material.Document;
        // A duplicate that elements still reference cannot be removed without reassigning every
        // geometry, compound-structure and paint reference, so the fix only deletes the unused copy.
        if (UsedMaterialIds.Get(documentQueries, document).Contains(material.Id)) return false;
        return DocumentValidation.CanDeleteElement(document, material.Id)
            && document.Delete(material.Id).Any();
    }
}
