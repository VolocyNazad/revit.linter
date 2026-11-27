using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;

internal sealed class UnusedSizeTablesDiagnostic(SizeTableReferenceCollector collector)
    : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.UnusedSizeTables;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = collector.Collect(targetDocument);
        if (references.Manager is null || references.TableNames.Count == 0) yield break;

        // A formula whose table argument cannot be resolved statically may refer to any table.
        // Reporting the other tables as unused would offer a deletion that can break the family.
        if (references.HasUnresolvedTableReferences) yield break;

        foreach (string tableName in references.TableNames)
        {
            if (references.IsReferenced(tableName)) continue;
            yield return new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["tableName"] = tableName });
        }
    }
}
