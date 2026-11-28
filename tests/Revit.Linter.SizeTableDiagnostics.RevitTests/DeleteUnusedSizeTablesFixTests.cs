using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.UnusedSizeTables;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.SizeTableDiagnostics.RevitTests;

public sealed class DeleteUnusedSizeTablesFixTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _familyDocument = TestFamilyDocuments.OpenTemplateFamily(Application);
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
        var fix = new DeleteUnusedSizeTablesFix();

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
        var fix = new DeleteUnusedSizeTablesFix();

        bool applied = Execute(familyDocument, fix);
        string[] names = SizeTableTestDocuments.GetManager(familyDocument)!
            .GetAllSizeTableNames()
            .ToArray();

        await Assert.That(applied).IsFalse();
        await Assert.That(names).Contains(tableName);
    }

    [Test]
    public async Task Is_available_when_all_table_names_are_literal()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", \"Length\", Width, Length)");
        var fix = new DeleteUnusedSizeTablesFix();

        bool available = fix.IsAvailable(familyDocument);

        await Assert.That(available).IsTrue();
    }

    [Test]
    public async Task Is_unavailable_when_a_table_name_comes_from_a_parameter()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter table = SizeTableTestDocuments.AddTextParameter(familyDocument, "Table");
        SizeTableTestDocuments.SetText(familyDocument, table, tableName);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(Table, \"Length\", Width, Length)");
        var fix = new DeleteUnusedSizeTablesFix();

        bool available = fix.IsAvailable(familyDocument);
        bool applied = Execute(familyDocument, fix);

        await Assert.That(available).IsFalse();
        await Assert.That(applied).IsFalse();
    }

    [Test]
    public async Task Is_unavailable_when_a_table_name_is_an_expression()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string firstTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        string secondTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument,
            result,
            $"size_lookup(if(Length > 0 mm, \"{firstTable}\", \"{secondTable}\"), \"Length\", Width, Length)");
        var fix = new DeleteUnusedSizeTablesFix();

        bool available = fix.IsAvailable(familyDocument);

        await Assert.That(available).IsFalse();
    }

    [Test]
    public async Task Exposes_confirmation_text()
    {
        var fix = new DeleteUnusedSizeTablesFix();

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
