using Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTable;

/// <summary>Finds formulas that name a size lookup table that is absent from the family.</summary>
internal sealed class MissingSizeTableDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.MissingSizeTable;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = SizeTableReferenceCollector.Collect(targetDocument);
        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);

        foreach (SizeLookupReference reference in references.LookupReferences)
        {
            if (!reference.TableResolved) continue;
            foreach (string tableName in reference.TableNames)
            {
                if (SizeTableLookup.FindTable(references.Manager, tableName) is not null) continue;

                string key = $"{reference.ParameterName}|{tableName}";
                if (!reported.Add(key)) continue;
                yield return new DiagnosticFeedback(
                    DiagnosticVerdict.NotValid,
                    new Dictionary<string, object>
                    {
                        ["parameterName"] = reference.ParameterName,
                        ["tableName"] = tableName
                    });
            }
        }
    }
}
