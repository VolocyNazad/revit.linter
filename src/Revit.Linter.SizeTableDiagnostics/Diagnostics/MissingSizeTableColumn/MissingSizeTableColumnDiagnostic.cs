using Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTableColumn;

/// <summary>Finds formulas that name a column absent from an existing size lookup table.</summary>
internal sealed class MissingSizeTableColumnDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.MissingSizeTableColumn;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = SizeTableReferenceCollector.Collect(targetDocument);
        if (references.Manager is null) yield break;

        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);
        foreach (SizeLookupReference reference in references.LookupReferences)
        {
            foreach (SizeLookupPair pair in reference.Pairs)
            {
                string? existingTable = SizeTableLookup.FindTable(references.Manager, pair.TableName);
                if (existingTable is null) continue;

                HashSet<string>? columns = SizeTableLookup.TryCollectColumnNames(
                    references.Manager, existingTable);
                if (columns is null || columns.Contains(pair.ColumnName)) continue;

                string key = $"{reference.ParameterName}|{pair.TableName}|{pair.ColumnName}";
                if (!reported.Add(key)) continue;
                yield return new DiagnosticFeedback(
                    DiagnosticVerdict.NotValid,
                    new Dictionary<string, object>
                    {
                        ["parameterName"] = reference.ParameterName,
                        ["tableName"] = pair.TableName,
                        ["columnName"] = pair.ColumnName
                    });
            }
        }
    }
}
