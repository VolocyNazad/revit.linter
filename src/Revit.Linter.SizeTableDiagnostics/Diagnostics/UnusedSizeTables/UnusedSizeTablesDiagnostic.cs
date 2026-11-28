using Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.UnusedSizeTables;

internal sealed class UnusedSizeTablesDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.UnusedSizeTables;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = SizeTableReferenceCollector.Collect(targetDocument);
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
