using System.Text;

namespace Revit.Linter.TypeCatalogDiagnostics.Diagnostics.FamilyTypeCatalog;

/// <summary>Reads and writes the comma-separated fields of a type catalog line.</summary>
internal static class FamilyTypeCatalogCsv
{
    /// <summary>Splits a line into unquoted fields.</summary>
    /// <remarks>
    /// A field may be quoted, and a quoted field can contain commas and doubled quotes, which stand
    /// for one quote character.
    /// </remarks>
    public static IReadOnlyList<string> ReadFields(string line)
    {
        List<string> fields = [];
        StringBuilder field = new();
        bool quoted = false;
        int index = 0;
        while (index < line.Length)
        {
            char character = line[index];
            if (quoted)
            {
                if (character != '"')
                {
                    field.Append(character);
                    index++;
                    continue;
                }
                if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    field.Append('"');
                    index += 2;
                    continue;
                }
                quoted = false;
                index++;
                continue;
            }

            if (character == '"')
            {
                quoted = true;
                index++;
                continue;
            }
            if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                index++;
                continue;
            }
            field.Append(character);
            index++;
        }
        fields.Add(field.ToString());
        return fields;
    }

    /// <summary>Wraps a field in quotes when it contains a separator or a quote.</summary>
    public static string FormatField(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    /// <summary>Joins already formatted fields into one line.</summary>
    public static string Join(IEnumerable<string> fields) => string.Join(",", fields);
}
