using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ManyFamilyTypes;

internal sealed class ManyFamilyTypesDiagnostic : IElementDiagnostic
{
    // Autodesk recommends a type catalog for a family with 6 or more types. The default type is
    // not ours to count, so the check waits for the sixth type the user added.
    internal const int Threshold = 6;

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ManyFamilyTypes;

    public IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement)
    {
        if (!document.IsFamilyDocument || targetElement is not Family || targetElement.Id != document.OwnerFamily.Id)
            return DiagnosticFeedbacks.Valid;
        if (!FamilyTypeCatalogReader.IsSavedFamilyFile(document)) return DiagnosticFeedbacks.Valid;

        int typeCount = FamilyTypeCatalogReader.ReadOwnTypeCount(document);
        if (typeCount < Threshold) return DiagnosticFeedbacks.Valid;

        // A family with many types of its own is reported whether or not it also has a type catalog:
        // Autodesk advises loading many types through the catalog instead of storing them in the
        // family. The catalog path is read separately, so the finding can name it.
        string? catalogPath = FamilyTypeCatalogReader.ReadCatalogPath(document);
        Dictionary<string, object> arguments = new()
        {
            ["familyName"] = FamilyTypeCatalogReader.ReadFamilyName(document),
            ["typeCount"] = typeCount,
        };
        if (catalogPath is not null) arguments["catalogName"] = Path.GetFileName(catalogPath);

        return DiagnosticFeedbacks.Of(new(DiagnosticVerdict.NotValid, arguments)
        {
            MessageFormat = ElementDiagnosticLocalizations.GetString(
                catalogPath is null ? "SHRD0046_message" : "SHRD0046_message_with_catalog"),
        });
    }
}
