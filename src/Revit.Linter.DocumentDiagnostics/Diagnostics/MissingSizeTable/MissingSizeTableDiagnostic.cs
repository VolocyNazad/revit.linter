using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTable;

/// <summary>Finds formulas and family types that name a size lookup table that is absent.</summary>
internal sealed class MissingSizeTableDiagnostic(SizeTableReferenceCollector collector)
    : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.MissingSizeTable;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = collector.Collect(targetDocument);
        HashSet<string> reported = new(StringComparer.OrdinalIgnoreCase);

        foreach (SizeTableTypeReference typeReference in references.TypeReferences)
        {
            if (SizeTableLookup.FindTable(references.Manager, typeReference.TableName) is not null) continue;

            string key = $"type|{typeReference.TypeName}|{typeReference.TableName}";
            if (!reported.Add(key)) continue;
            yield return new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object>
                {
                    ["typeName"] = typeReference.TypeName,
                    ["tableName"] = typeReference.TableName
                })
            {
                MessageFormat = DocumentDiagnosticLocalizations.GetString("DOC005_missingTypeTableMessage")
            };
        }

        foreach (SizeLookupReference reference in references.LookupReferences)
        {
            if (!reference.TableResolved) continue;
            foreach (string tableName in reference.TableNames)
            {
                if (SizeTableLookup.FindTable(references.Manager, tableName) is not null) continue;

                string key = $"formula|{reference.ParameterName}|{tableName}";
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
