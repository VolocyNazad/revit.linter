using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypesMissingFromCatalog;

/// <summary>Adds the family types the catalog lacks and removes them from the family.</summary>
internal sealed class AddMissingTypesToCatalog : IElementFix, IElementFixConfirmation
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypesMissingFromCatalog;

    public string Value => ElementDiagnosticLocalizations.GetString("addMissingTypesToCatalog_fix");

    public string ConfirmationMessage =>
        ElementDiagnosticLocalizations.GetString("addMissingTypesToCatalog_confirmationMessage");

    public string ConfirmationButtonText =>
        ElementDiagnosticLocalizations.GetString("addMissingTypesToCatalog_confirmationButton");

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
        List<string> missing = FamilyTypeCatalogReader.ReadTypeNames(document)
            .Where(name => !catalog.Contains(name))
            .ToList();
        if (missing.Count == 0) return false;

        List<FamilyType> missingTypes = missing
            .Select(name => FindType(document, name))
            .OfType<FamilyType>()
            .ToList();
        List<string> rows = FamilyTypeCatalogWriter.BuildRows(document, columns, missingTypes);
        if (rows.Count == 0) return false;

        DeleteMissingTypes(document, missing);
        return FamilyTypeCatalogWriter.TryWrite(path, [.. lines, .. rows]);
    }

    // The one type Revit always keeps stays in the family even when its row is added to the catalog.
    private static void DeleteMissingTypes(Document document, IReadOnlyList<string> missing)
    {
        FamilyType? keep = FamilyTypeCatalogWriter.FindTypeToKeep(document);
        string? keepName = keep?.Name;
        foreach (string name in missing)
        {
            if (string.Equals(name, keepName, StringComparison.OrdinalIgnoreCase)) continue;
            if (FindType(document, name) is not { } type) continue;
            document.FamilyManager.CurrentType = type;
            document.FamilyManager.DeleteCurrentType();
        }
    }

    private static FamilyType? FindType(Document document, string name) =>
        document.FamilyManager.Types
            .Cast<FamilyType>()
            .FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase));
}
