using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;
using Revit.Linter.DocumentDiagnostics.Diagnostics.UnusedSizeTables;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DeleteUnusedSizeTablesFixTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _familyDocument = FamilyTestDocuments.OpenTemplateFamily(Application);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _familyDocument?.Close(false);
    }

    [Test]
    public async Task Removes_unused_tables_and_keeps_referenced_ones()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string usedTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        string unusedTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{usedTable}\", \"Length\", Width, Length)");
        var fix = new DeleteUnusedSizeTablesFix(
            new SizeTableReferenceCollector(TestDocumentQueries.Create()));

        bool applied = Execute(familyDocument, fix);
        string[] names = SizeTableTestDocuments.GetManager(familyDocument)!
            .GetAllSizeTableNames()
            .ToArray();

        await Assert.That(applied).IsTrue();
        await Assert.That(names).Contains(usedTable);
        await Assert.That(names).DoesNotContain(unusedTable);
    }

    [Test]
    public async Task Returns_false_when_every_table_is_referenced()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", \"Length\", Width, Length)");
        var fix = new DeleteUnusedSizeTablesFix(
            new SizeTableReferenceCollector(TestDocumentQueries.Create()));

        bool applied = Execute(familyDocument, fix);
        string[] names = SizeTableTestDocuments.GetManager(familyDocument)!
            .GetAllSizeTableNames()
            .ToArray();

        await Assert.That(applied).IsFalse();
        await Assert.That(names).Contains(tableName);
    }

    [Test]
    public async Task Exposes_confirmation_text()
    {
        var fix = new DeleteUnusedSizeTablesFix(
            new SizeTableReferenceCollector(TestDocumentQueries.Create()));

        await Assert.That(fix.ConfirmationMessage).IsNotEmpty();
        await Assert.That(fix.ConfirmationButtonText).IsNotEmpty();
    }

    private static bool Execute(Document familyDocument, DeleteUnusedSizeTablesFix fix)
    {
        using Transaction transaction = new(familyDocument, "Delete unused size tables");
        transaction.Start();
        bool applied = fix.Execute(familyDocument);
        transaction.Commit();
        return applied;
    }
}
