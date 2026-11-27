using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;

/// <summary>Removes every size lookup table that no family type or formula refers to.</summary>
internal sealed class DeleteUnusedSizeTablesFix(SizeTableReferenceCollector collector)
    : IDocumentFix, IDocumentFixConfirmation
{
    public DocumentDiagnosticId Identity => DocumentDiagnosticIdCollector.UnusedSizeTables;

    public string Value => DocumentDiagnosticLocalizations.GetString("deleteUnusedSizeTables_fix");

    public string ConfirmationMessage =>
        DocumentDiagnosticLocalizations.GetString("deleteUnusedSizeTables_confirmationMessage");

    public string ConfirmationButtonText =>
        DocumentDiagnosticLocalizations.GetString("deleteUnusedSizeTables_confirmationButton");

    public bool Execute(Document targetDocument)
    {
        SizeTableReferenceCollection references = collector.Collect(targetDocument);
        if (references.Manager is null || references.HasUnresolvedTableReferences) return false;

        string[] unusedTableNames = references.TableNames
            .Where(tableName => !references.IsReferenced(tableName))
            .ToArray();
        if (unusedTableNames.Length == 0) return false;

        bool removedAny = false;
        foreach (string tableName in unusedTableNames)
        {
            // A removal can invalidate the manager proxy, so the next one takes a fresh manager.
            FamilySizeTableManager? manager = targetDocument.OwnerFamily is { } family
                ? FamilySizeTableManager.GetFamilySizeTableManager(targetDocument, family.Id)
                : null;
            if (manager is null) break;
            removedAny |= manager.RemoveSizeTable(tableName);
        }
        return removedAny;
    }
}
