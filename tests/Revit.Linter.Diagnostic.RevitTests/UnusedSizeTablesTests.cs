using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class UnusedSizeTablesTests : RevitApiTest
{
    private Document? _projectDocument;
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocuments()
    {
        _projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        _familyDocument = FamilyTestDocuments.OpenTemplateFamily(Application);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocuments()
    {
        _projectDocument?.Close(false);
        _familyDocument?.Close(false);
    }

    [Test]
    public async Task Project_document_is_not_relevant()
    {
        var filter = new UnusedSizeTablesDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(_projectDocument!)).IsFalse();
    }

    [Test]
    public async Task Family_without_tables_has_no_findings()
    {
        await Assert.That(_familyDocument).IsNotNull();
        var diagnostic = new UnusedSizeTablesDiagnostic(TestDocumentQueries.Create());

        IReadOnlyList<DiagnosticFeedback> feedback = diagnostic.Execute(_familyDocument!).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Imported_but_unreferenced_table_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        string tablePath = Path.Combine(Path.GetTempPath(), $"UnusedSizeTable{Guid.NewGuid():N}.csv");
        try
        {
            WriteTableCsv(tablePath);
            Family family = _familyDocument!.OwnerFamily;
            var errorInfo = new FamilySizeTableErrorInfo();
            bool imported;
            string tableName = Path.GetFileNameWithoutExtension(tablePath);
            using (Transaction transaction = new(_familyDocument, "Import test size table"))
            {
                transaction.Start();
                AddLengthParameter(_familyDocument);
                FamilySizeTableManager.CreateFamilySizeTableManager(_familyDocument, family.Id);
                FamilySizeTableManager manager =
                    FamilySizeTableManager.GetFamilySizeTableManager(_familyDocument, family.Id)!;
                imported = manager.ImportSizeTable(_familyDocument, tablePath, errorInfo);
                transaction.Commit();
            }

            if (!imported)
                throw new InvalidOperationException(
                    $"Size table import failed: {errorInfo.FamilySizeTableErrorType} " +
                    $"(file: '{errorInfo.FilePath}', column: {errorInfo.InvalidColumnIndex}, " +
                    $"header: '{errorInfo.InvalidHeaderText}', row: {errorInfo.InvalidRowIndex}).");
            var diagnostic = new UnusedSizeTablesDiagnostic(TestDocumentQueries.Create());
            IReadOnlyList<DiagnosticFeedback> feedback = diagnostic.Execute(_familyDocument).ToArray();

            await Assert.That(feedback.Count()).IsEqualTo(1);
            await Assert.That(feedback[0].Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
            await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        }
        finally
        {
            if (File.Exists(tablePath)) File.Delete(tablePath);
        }
    }

    // Sync write: File.WriteAllTextAsync does not exist on net48, while the async S6966 suggestion
    // applies to async callers only, so an isolated sync helper keeps every target framework clean.
    // The header starts with an empty key column, like the tables shipped under Lookup Tables.
    private static void WriteTableCsv(string tablePath) =>
        File.WriteAllText(tablePath, ",Length##length##millimeters,Width##length##millimeters\nS,100,50\n");

    // Size table columns must match real family parameters, otherwise the import is rejected.
    private static void AddLengthParameter(Document familyDocument)
    {
        FamilyManager manager = familyDocument.FamilyManager;
#if IS2021
        manager.AddParameter("Length", BuiltInParameterGroup.PG_DATA, ParameterType.Length, false);
        manager.AddParameter("Width", BuiltInParameterGroup.PG_DATA, ParameterType.Length, false);
#else
        manager.AddParameter("Length", GroupTypeId.Data, SpecTypeId.Length, false);
        manager.AddParameter("Width", GroupTypeId.Data, SpecTypeId.Length, false);
#endif
    }
}
