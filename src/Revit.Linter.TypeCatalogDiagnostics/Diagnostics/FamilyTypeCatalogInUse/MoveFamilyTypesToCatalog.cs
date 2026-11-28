using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

/// <summary>Adds the family types to the catalog and removes them from the family.</summary>
internal sealed class MoveFamilyTypesToCatalog : IElementFix, IElementFixConfirmation
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public string Value => TypeCatalogDiagnosticLocalizations.GetString("moveFamilyTypesToCatalog_fix");

    public string ConfirmationMessage =>
        TypeCatalogDiagnosticLocalizations.GetString("moveFamilyTypesToCatalog_confirmationMessage");

    public string ConfirmationButtonText =>
        TypeCatalogDiagnosticLocalizations.GetString("moveFamilyTypesToCatalog_confirmationButton");

    public bool Execute(Element targetElement)
    {
        Document document = targetElement.Document;
        if (!document.IsFamilyDocument || targetElement.Id != document.OwnerFamily.Id) return false;
        if (FamilyTypeCatalogReader.ReadCatalogPath(document) is not { } path) return false;
        if (FamilyTypeCatalogReader.TryRead(document) is not { TypeNames.Count: > 0 } catalog) return false;

        List<string> lines;
        try
        {
            lines = File.ReadAllLines(path).ToList();
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        IReadOnlyList<FamilyTypeCatalogColumn> columns = FamilyTypeCatalogWriter.ReadColumns(lines);
        List<FamilyType> missing = document.FamilyManager.Types.Cast<FamilyType>()
            .Where(type => !string.IsNullOrWhiteSpace(type.Name) && !catalog.Contains(type.Name))
            .ToList();
        List<string> rows = FamilyTypeCatalogWriter.BuildRows(document, columns, missing);

        FamilyType? keep = document.FamilyManager.CurrentType ?? FamilyTypeCatalogWriter.FindTypeToKeep(document);
        if (keep is null) return false;
        FamilyTypeCatalogWriter.DeleteTypesExcept(document, keep);

        return rows.Count == 0 || FamilyTypeCatalogWriter.TryWrite(path, [.. lines, .. rows]);
    }
}
