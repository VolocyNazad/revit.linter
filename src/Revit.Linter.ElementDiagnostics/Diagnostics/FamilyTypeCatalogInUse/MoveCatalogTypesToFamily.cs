using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalogInUse;

/// <summary>Creates every catalog type in the family and deletes the catalog.</summary>
internal sealed class MoveCatalogTypesToFamily : IElementFix, IElementFixConfirmation
{
    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.FamilyTypeCatalogInUse;

    public string Value => ElementDiagnosticLocalizations.GetString("moveCatalogTypesToFamily_fix");

    public string ConfirmationMessage =>
        ElementDiagnosticLocalizations.GetString("moveCatalogTypesToFamily_confirmationMessage");

    public string ConfirmationButtonText =>
        ElementDiagnosticLocalizations.GetString("moveCatalogTypesToFamily_confirmationButton");

    public bool Execute(Element targetElement)
    {
        Document document = targetElement.Document;
        if (!document.IsFamilyDocument || targetElement.Id != document.OwnerFamily.Id) return false;
        if (FamilyTypeCatalogReader.ReadCatalogPath(document) is not { } path) return false;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
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
        HashSet<string> names = new(
            document.FamilyManager.Types.Cast<FamilyType>()
                .Select(type => type.Name?.Trim() ?? string.Empty),
            StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach ((string name, IReadOnlyList<string> fields) in ReadRows(lines))
            {
                if (names.Contains(name)) continue;
                FamilyType type = document.FamilyManager.NewType(name);
                if (!FamilyTypeCatalogValues.Apply(document, type, columns, fields)) return false;
            }
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            return false;
        }

        return TryDelete(path);
    }

    private static IEnumerable<(string Name, IReadOnlyList<string> Fields)> ReadRows(IReadOnlyList<string> lines)
    {
        bool headerRead = false;
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!headerRead)
            {
                headerRead = true;
                continue;
            }

            IReadOnlyList<string> fields = FamilyTypeCatalogCsv.ReadFields(line);
            string name = fields[0].Trim();
            if (name.Length > 0) yield return (name, fields);
        }
    }

    private static bool TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}
