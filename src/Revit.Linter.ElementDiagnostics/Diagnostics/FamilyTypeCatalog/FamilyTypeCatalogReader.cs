namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

internal static class FamilyTypeCatalogReader
{
    // Revit pairs a family file with a type catalog only for a saved .rfa: the catalog is the .txt
    // file with the same base name in the same folder. A family document being edited has no path,
    // and a template is not loaded through a catalog, so both stay unchecked.
    public static bool IsSavedFamilyFile(Document document) =>
        !string.IsNullOrWhiteSpace(document.PathName)
        && string.Equals(Path.GetExtension(document.PathName), ".rfa", StringComparison.OrdinalIgnoreCase);

    // A family names itself after its file: OwnerFamily.Name stays empty in a family document.
    public static string ReadFamilyName(Document document) =>
        IsSavedFamilyFile(document) ? Path.GetFileNameWithoutExtension(document.PathName) : string.Empty;

    // Revit keeps one type that stands for the family itself. A template gives it a blank name, and
    // NewType rejects a blank name, so a blank name belongs to that type only. A type renamed after
    // the family keeps its role. That type is not a type of its own.
    public static bool IsOwnTypeName(string? name, string familyName)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length > 0
            && !string.Equals(trimmed, familyName, StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> ReadOwnTypeNames(Document document)
    {
        string familyName = ReadFamilyName(document);
        List<string> names = [];
        foreach (FamilyType type in document.FamilyManager.Types.Cast<FamilyType>())
        {
            string name = type.Name?.Trim() ?? string.Empty;
            if (IsOwnTypeName(name, familyName)) names.Add(name);
        }
        return names;
    }

    public static int ReadOwnTypeCount(Document document) => ReadOwnTypeNames(document).Count;

    // Every family type a catalog can list by name. The type with a blank name has no name to list.
    public static IReadOnlyList<string> ReadTypeNames(Document document)
    {
        List<string> names = [];
        foreach (FamilyType type in document.FamilyManager.Types.Cast<FamilyType>())
        {
            string name = type.Name?.Trim() ?? string.Empty;
            if (name.Length > 0) names.Add(name);
        }
        return names;
    }

    // The catalog path exists only for a saved family file with a .txt file next to it. The path is
    // returned even when the file cannot be read, so a finding can name the catalog it saw.
    public static string? ReadCatalogPath(Document document)
    {
        if (!IsSavedFamilyFile(document)) return null;
        string catalogPath = Path.ChangeExtension(document.PathName, ".txt");
        return File.Exists(catalogPath) ? catalogPath : null;
    }

    public static FamilyTypeCatalogFile? TryRead(Document document)
    {
        if (ReadCatalogPath(document) is not { } catalogPath) return null;

        try
        {
            return TryReadCatalog(
                File.ReadAllLines(catalogPath),
                out IReadOnlyList<string> parameterNames,
                out IReadOnlyList<string> typeNames)
                ? new FamilyTypeCatalogFile(Path.GetFileName(catalogPath), parameterNames, typeNames)
                : null;
        }
        catch (IOException)
        {
            // A catalog that cannot be read is treated as absent: the check reports only what it can
            // prove, and a locked or incomplete file must not fail the whole diagnostic run.
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            // Same as an unreadable catalog: the check reports only what it can prove.
            return null;
        }
        catch (ArgumentException)
        {
            // A path the file system rejects is not a catalog this check can read.
            return null;
        }
        catch (NotSupportedException)
        {
            // A path the file system does not support is not a catalog this check can read.
            return null;
        }
    }

    // The first non-empty line is the header: the type name column has no name, the rest are
    // parameter columns written as Name##Spec##Unit. Every following line is one type, and its name
    // is the first field.
    private static bool TryReadCatalog(
        IReadOnlyList<string> lines,
        out IReadOnlyList<string> parameterNames,
        out IReadOnlyList<string> typeNames)
    {
        parameterNames = [];
        typeNames = [];
        bool headerRead = false;
        List<string> parsedNames = [];
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!headerRead)
            {
                headerRead = true;
                parameterNames = FamilyTypeCatalogCsv.ReadFields(line)
                    .Skip(1)
                    .Select(ReadColumnName)
                    .OfType<string>()
                    .ToList();
                continue;
            }

            string typeName = FamilyTypeCatalogCsv.ReadFields(line)[0].Trim();
            if (typeName.Length > 0) parsedNames.Add(typeName);
        }

        if (!headerRead) return false;
        typeNames = parsedNames;
        return true;
    }

    // The header names the parameter first and may append its spec and unit after "##".
    private static string? ReadColumnName(string header)
    {
        int suffix = header.IndexOf("##", StringComparison.Ordinal);
        string name = (suffix < 0 ? header : header.Substring(0, suffix)).Trim();
        return name.Length == 0 ? null : name;
    }

}
