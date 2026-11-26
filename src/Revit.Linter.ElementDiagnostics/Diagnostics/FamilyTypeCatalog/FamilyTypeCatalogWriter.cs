using System.Globalization;
using System.Text;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Writes a type catalog next to a saved family file and formats the values it holds.</summary>
internal static class FamilyTypeCatalogWriter
{
    /// <summary>Writes the lines to the catalog, replacing the file only when every line is written.</summary>
    /// <param name="path">The catalog path.</param>
    /// <param name="lines">The complete catalog content.</param>
    /// <returns><see langword="true"/> when the file was replaced.</returns>
    public static bool TryWrite(string path, IReadOnlyList<string> lines)
    {
        string temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllLines(temporaryPath, lines, ReadEncoding(path));
            // File.Move has no overwrite overload on net48, where this project also builds.
            File.Copy(temporaryPath, path, overwrite: true);
            File.Delete(temporaryPath);
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
        finally
        {
            try
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                // A temporary file left behind is harmless; the catalog itself is complete.
            }
            catch (UnauthorizedAccessException)
            {
                // Same as above: the fix reports success only for the catalog it wrote.
            }
        }
    }

    /// <summary>Reads the parameter columns of an existing catalog.</summary>
    public static IReadOnlyList<FamilyTypeCatalogColumn> ReadColumns(IReadOnlyList<string> lines)
    {
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            return FamilyTypeCatalogCsv.ReadFields(line)
                .Skip(1)
                .Select(FamilyTypeCatalogColumn.FromHeader)
                .ToList();
        }
        return [];
    }

    /// <summary>Builds the header and one row per type for a new catalog.</summary>
    public static List<string> BuildLines(
        Document document,
        IReadOnlyList<FamilyTypeCatalogColumn> columns,
        IReadOnlyList<FamilyType> types)
    {
        List<string> lines = [];
        List<string> header = [string.Empty];
        header.AddRange(columns.Select(column => FamilyTypeCatalogCsv.FormatField(column.Header)));
        lines.Add(FamilyTypeCatalogCsv.Join(header));
        lines.AddRange(BuildRows(document, columns, types));
        return lines;
    }

    /// <summary>Builds one catalog row per type, without the header.</summary>
    public static List<string> BuildRows(
        Document document,
        IReadOnlyList<FamilyTypeCatalogColumn> columns,
        IReadOnlyList<FamilyType> types)
    {
        Dictionary<string, FamilyParameter> parameters = ReadParameters(document);
        List<string> rows = [];
        foreach (FamilyType type in types)
        {
            List<string> fields = [FamilyTypeCatalogCsv.FormatField(type.Name)];
            foreach (FamilyTypeCatalogColumn column in columns)
            {
                parameters.TryGetValue(column.ParameterName, out FamilyParameter? parameter);
                fields.Add(FamilyTypeCatalogCsv.FormatField(FormatValue(document, type, parameter, column)));
            }
            rows.Add(FamilyTypeCatalogCsv.Join(fields));
        }
        return rows;
    }

    /// <summary>Formats one catalog cell for a type.</summary>
    public static string FormatValue(
        Document document,
        FamilyType type,
        FamilyParameter? parameter,
        FamilyTypeCatalogColumn column)
    {
        if (parameter is null || !type.HasValue(parameter)) return string.Empty;

        switch (parameter.StorageType)
        {
            case StorageType.Double:
                if (type.AsDouble(parameter) is not { } raw) return string.Empty;
                if (column.Unit is { } unit) raw = UnitUtils.ConvertFromInternalUnits(raw, unit);
                return raw.ToString("R", CultureInfo.InvariantCulture);
            case StorageType.Integer:
                return (type.AsInteger(parameter) ?? 0).ToString(CultureInfo.InvariantCulture);
            case StorageType.String:
                return type.AsString(parameter) ?? string.Empty;
            case StorageType.ElementId:
                return type.AsElementId(parameter) is { } id ? ReadElementName(document, id) : string.Empty;
            default:
                return string.Empty;
        }
    }

    /// <summary>Finds a family parameter by its catalog name.</summary>
    public static FamilyParameter? FindParameter(Document document, string name) =>
        document.FamilyManager.Parameters
            .Cast<FamilyParameter>()
            .FirstOrDefault(parameter => string.Equals(parameter.Definition.Name, name, StringComparison.Ordinal));

    /// <summary>Reads the type parameters a new catalog can set and at least one type has a value for.</summary>
    /// <remarks>
    /// An empty built-in column makes Revit reject the catalog, so a parameter no type has a value
    /// for is left out.
    /// </remarks>
    public static IReadOnlyList<FamilyTypeCatalogColumn> ReadSettableColumns(
        Document document,
        IReadOnlyList<FamilyType> types)
    {
        List<FamilyTypeCatalogColumn> columns = [];
        foreach (FamilyParameter parameter in document.FamilyManager.Parameters.Cast<FamilyParameter>())
        {
            if (parameter.IsInstance
                || parameter.IsReadOnly
                || parameter.IsReporting
                || !string.IsNullOrWhiteSpace(parameter.Formula))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(parameter.Definition.Name)) continue;
            if (!types.Any(type => type.HasValue(parameter))) continue;
            columns.Add(FamilyTypeCatalogColumn.FromParameter(parameter));
        }
        return columns;
    }

    /// <summary>Finds the type Revit always keeps: the unnamed one, then the one named after the family.</summary>
    public static FamilyType? FindTypeToKeep(Document document)
    {
        string familyName = FamilyTypeCatalogReader.ReadFamilyName(document);
        FamilyType? named = null;
        foreach (FamilyType type in document.FamilyManager.Types.Cast<FamilyType>())
        {
            string name = type.Name?.Trim() ?? string.Empty;
            if (name.Length == 0) return type;
            if (string.Equals(name, familyName, StringComparison.OrdinalIgnoreCase)) named = type;
        }
        return named ?? document.FamilyManager.Types.Cast<FamilyType>().FirstOrDefault();
    }

    /// <summary>Deletes every family type except the one Revit keeps.</summary>
    public static void DeleteTypesExcept(Document document, FamilyType keep)
    {
        FamilyManager manager = document.FamilyManager;
        string keepName = keep.Name;
        foreach (FamilyType type in manager.Types.Cast<FamilyType>().ToList())
        {
            if (string.Equals(type.Name, keepName, StringComparison.Ordinal)) continue;
            manager.CurrentType = type;
            manager.DeleteCurrentType();
        }
    }

    private static Dictionary<string, FamilyParameter> ReadParameters(Document document)
    {
        Dictionary<string, FamilyParameter> parameters = new(StringComparer.Ordinal);
        foreach (FamilyParameter parameter in document.FamilyManager.Parameters.Cast<FamilyParameter>())
        {
            string name = parameter.Definition.Name;
            if (!string.IsNullOrWhiteSpace(name)) parameters[name] = parameter;
        }
        return parameters;
    }

    private static string ReadElementName(Document document, ElementId id)
    {
        if (id == ElementId.InvalidElementId) return string.Empty;
        return document.GetElement(id) switch
        {
            FamilySymbol symbol => $"{symbol.Family.Name} : {symbol.Name}",
            Element element => element.Name ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static Encoding ReadEncoding(string path)
    {
        if (!File.Exists(path)) return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
        try
        {
            using FileStream stream = File.OpenRead(path);
            int first = stream.ReadByte();
            int second = stream.ReadByte();
            int third = stream.ReadByte();
            if (first == 0xFF && second == 0xFE) return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            if (first == 0xFE && second == 0xFF) return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            if (first == 0xEF && second == 0xBB && third == 0xBF) return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }
        catch (IOException)
        {
            // The catalog will be written in UTF-8 when its current encoding cannot be read.
        }
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}
