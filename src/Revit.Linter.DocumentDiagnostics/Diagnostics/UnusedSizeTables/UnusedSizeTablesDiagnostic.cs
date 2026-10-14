namespace Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;

internal sealed class UnusedSizeTablesDiagnostic : IDocumentDiagnostic
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.UnusedSizeTables;

    public IEnumerable<DiagnosticFeedback> Execute(Document targetDocument)
    {
        // Size tables are stored inside the family document itself (see FamilySizeTableManager):
        // no file-system scan is involved.
        Family? family = targetDocument.OwnerFamily;
        FamilySizeTableManager? manager = family is null
            ? null
            : FamilySizeTableManager.GetFamilySizeTableManager(targetDocument, family.Id);
        if (manager is null || manager.NumberOfSizeTables == 0) yield break;

        HashSet<string> referencedNames = CollectReferencedTableNames(targetDocument);
        foreach (string tableName in manager.GetAllSizeTableNames().Where(tableName => !IsReferenced(tableName, referencedNames)))
            yield return new DiagnosticFeedback(
                DiagnosticVerdict.NotValid,
                new Dictionary<string, object> { ["tableName"] = tableName });
    }

    private static HashSet<string> CollectReferencedTableNames(Document targetDocument)
    {
        HashSet<string> referencedNames = new(StringComparer.OrdinalIgnoreCase);
        IList<Element> symbols = new FilteredElementCollector(targetDocument)
            .OfClass(typeof(FamilySymbol))
            .ToElements();
        foreach (Element symbol in symbols)
        {
            string? rawName = symbol
                .get_Parameter(BuiltInParameter.RBS_LOOKUP_TABLE_NAME)
                ?.AsString();
            // Explicit null check: string.IsNullOrWhiteSpace carries no NotNullWhen contract
            // on net48, so it alone does not narrow the nullable on every target framework.
            if (rawName is null || string.IsNullOrWhiteSpace(rawName)) continue;
            string tableName = rawName.Trim();
            referencedNames.Add(tableName);
            referencedNames.Add(Path.GetFileName(tableName));
            referencedNames.Add(Path.GetFileNameWithoutExtension(tableName));
        }
        return referencedNames;
    }

    private static bool IsReferenced(string tableName, HashSet<string> referencedNames) =>
        referencedNames.Contains(tableName)
        || referencedNames.Contains(Path.GetFileNameWithoutExtension(tableName));
}
