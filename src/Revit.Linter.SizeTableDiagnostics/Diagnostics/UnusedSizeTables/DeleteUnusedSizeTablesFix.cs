using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.SizeTables;

namespace Revit.Linter.SizeTableDiagnostics.Diagnostics.UnusedSizeTables;

/// <summary>Removes every size lookup table that no formula refers to when all table names are literal.</summary>
internal sealed class DeleteUnusedSizeTablesFix
    : IDocumentFix, IDocumentFixConfirmation, IDocumentFixAvailability
{
    public DocumentDiagnosticId Identity => SizeTableDiagnosticIdCollector.UnusedSizeTables;

    public string Value => SizeTableDiagnosticLocalizations.GetString("deleteUnusedSizeTables_fix");

    public string ConfirmationMessage =>
        SizeTableDiagnosticLocalizations.GetString("deleteUnusedSizeTables_confirmationMessage");

    public string ConfirmationButtonText =>
        SizeTableDiagnosticLocalizations.GetString("deleteUnusedSizeTables_confirmationButton");

    public bool IsAvailable(Document targetDocument) => TryCollect(targetDocument, out _);

    public bool Execute(Document targetDocument)
    {
        if (!TryCollect(targetDocument, out SizeTableReferenceCollection references)) return false;

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

    // A formula whose table name comes from a parameter can change in a project, so the family
    // document cannot prove that a table is unused. Only literal table names make the deletion safe.
    private static bool TryCollect(
        Document targetDocument, out SizeTableReferenceCollection references)
    {
        try
        {
            references = SizeTableReferenceCollector.Collect(targetDocument);
        }
        catch (Exception)
        {
            references = SizeTableReferenceCollection.Empty;
            return false;
        }

        return references.Manager is not null
            && !references.HasUnresolvedTableReferences
            && references.AllTableNamesAreLiteral;
    }
}
