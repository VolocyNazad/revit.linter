using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTableColumn;

/// <summary>Finds formulas that name a column absent from an existing size lookup table.</summary>
internal sealed class MissingSizeTableColumnDiagnostic(SizeTableReferenceCollector collector)
    : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.MissingSizeTableColumn;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = collector.Collect(targetDocument);
        if (references.Manager is null) yield break;

        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);
        foreach (SizeLookupReference reference in references.LookupReferences)
        {
            if (!reference.TableResolved || !reference.ColumnResolved) continue;
            // Two parameter-driven values are resolved as separate sets, so their pairing is
            // unknown. Checking the cross product could report a column that exists in the
            // table the type actually uses, so this case is left to the table check.
            if (!reference.TableIsLiteral && !reference.ColumnIsLiteral
                && (reference.TableNames.Count > 1 || reference.ColumnNames.Count > 1))
            {
                continue;
            }

            foreach (string tableName in reference.TableNames)
            {
                string? existingTable = SizeTableLookup.FindTable(references.Manager, tableName);
                if (existingTable is null) continue;

                HashSet<string>? columns = SizeTableLookup.TryCollectColumnNames(
                    references.Manager, existingTable);
                if (columns is null) continue;
                foreach (string columnName in reference.ColumnNames)
                {
                    if (columns.Contains(columnName)) continue;

                    string key = $"{reference.ParameterName}|{tableName}|{columnName}";
                    if (!reported.Add(key)) continue;
                    yield return new DiagnosticFeedback(
                        DiagnosticVerdict.NotValid,
                        new Dictionary<string, object>
                        {
                            ["parameterName"] = reference.ParameterName,
                            ["tableName"] = tableName,
                            ["columnName"] = columnName
                        });
                }
            }
        }
    }
}
