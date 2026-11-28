using Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalogMissingParameters;

/// <summary>Adds the family parameters the catalog header lacks and fills their values.</summary>
internal sealed class AddMissingParametersToCatalog : IElementFix
{
    public ElementDiagnosticId Identity => TypeCatalogDiagnosticIdCollector.FamilyTypeCatalogMissingParameters;

    public string Value => TypeCatalogDiagnosticLocalizations.GetString("addMissingParametersToCatalog_fix");

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

        List<FamilyTypeCatalogColumn> columns = document.FamilyManager.Parameters
            .Cast<FamilyParameter>()
            .Where(FamilyTypeCatalogParameters.IsSetByCatalog)
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Definition.Name))
            .Where(parameter => !catalog.ContainsParameter(parameter.Definition.Name))
            .Select(FamilyTypeCatalogColumn.FromParameter)
            .ToList();
        if (columns.Count == 0) return false;

        if (Rewrite(document, lines, columns) is not { } output) return false;
        return FamilyTypeCatalogWriter.TryWrite(path, output);
    }

    private static List<string>? Rewrite(
        Document document,
        IReadOnlyList<string> lines,
        IReadOnlyList<FamilyTypeCatalogColumn> columns)
    {
        List<string> output = [];
        bool headerWritten = false;
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                output.Add(line);
                continue;
            }

            IReadOnlyList<string> fields = FamilyTypeCatalogCsv.ReadFields(line);
            if (!headerWritten)
            {
                headerWritten = true;
                List<string> header = fields.Select(FamilyTypeCatalogCsv.FormatField).ToList();
                header.AddRange(columns.Select(column => FamilyTypeCatalogCsv.FormatField(column.Header)));
                output.Add(FamilyTypeCatalogCsv.Join(header));
                continue;
            }

            FamilyType? type = FindType(document, fields[0]);
            List<string> row = fields.Select(FamilyTypeCatalogCsv.FormatField).ToList();
            foreach (FamilyTypeCatalogColumn column in columns)
            {
                if (type is null)
                {
                    row.Add(string.Empty);
                    continue;
                }
                FamilyParameter? parameter = FamilyTypeCatalogWriter.FindParameter(document, column.ParameterName);
                row.Add(FamilyTypeCatalogCsv.FormatField(
                    FamilyTypeCatalogWriter.FormatValue(document, type, parameter, column)));
            }
            output.Add(FamilyTypeCatalogCsv.Join(row));
        }
        return headerWritten ? output : null;
    }

    private static FamilyType? FindType(Document document, string name) =>
        document.FamilyManager.Types
            .Cast<FamilyType>()
            .FirstOrDefault(type => string.Equals(type.Name, name, StringComparison.OrdinalIgnoreCase));
}
