using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.ManyFamilyTypes;

/// <summary>Writes the family types to a new catalog and removes them from the family.</summary>
internal sealed class CreateCatalogAndRemoveTypes : IElementFix, IElementFixConfirmation
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.ManyFamilyTypes;

    public string Value => ElementDiagnosticLocalizations.GetString("createCatalogAndRemoveTypes_fix");

    public string ConfirmationMessage =>
        ElementDiagnosticLocalizations.GetString("createCatalogAndRemoveTypes_confirmationMessage");

    public string ConfirmationButtonText =>
        ElementDiagnosticLocalizations.GetString("createCatalogAndRemoveTypes_confirmationButton");

    public bool Execute(Element targetElement)
    {
        Document document = targetElement.Document;
        if (!document.IsFamilyDocument || targetElement.Id != document.OwnerFamily.Id) return false;
        if (!FamilyTypeCatalogReader.IsSavedFamilyFile(document)) return false;

        // The catalog is built from the types as they are now; the types then leave the family. The
        // caller's transaction restores them when the file cannot be written.
        List<FamilyType> types = document.FamilyManager.Types.Cast<FamilyType>().ToList();
        IReadOnlyList<FamilyTypeCatalogColumn> columns =
            FamilyTypeCatalogWriter.ReadSettableColumns(document, types);
        List<string> lines = FamilyTypeCatalogWriter.BuildLines(document, columns, types);

        if ((document.FamilyManager.CurrentType ?? FamilyTypeCatalogWriter.FindTypeToKeep(document)) is not { } keep)
            return false;
        FamilyTypeCatalogWriter.DeleteTypesExcept(document, keep);
        return FamilyTypeCatalogWriter.TryWrite(Path.ChangeExtension(document.PathName, ".txt"), lines);
    }
}
