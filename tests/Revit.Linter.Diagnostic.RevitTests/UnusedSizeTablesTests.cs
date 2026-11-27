using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;
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

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(_familyDocument!).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Imported_but_unreferenced_table_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        string tableName = SizeTableTestDocuments.ImportTable(
            _familyDocument!, SizeTableTestDocuments.ProbeTableCsv);

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(_familyDocument!).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That(feedback[0].Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
    }

    [Test]
    public async Task Table_referenced_by_literal_formula_is_not_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", \"Length\", Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Table_referenced_by_table_name_parameter_is_not_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter tableNameParameter = SizeTableTestDocuments.AddTextParameter(familyDocument, "TableName");
        SizeTableTestDocuments.SetText(familyDocument, tableNameParameter, tableName);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(TableName, \"Length\", Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Table_with_unresolved_formula_reference_is_not_reported()
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

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    private static UnusedSizeTablesDiagnostic CreateDiagnostic() =>
        new(new SizeTableReferenceCollector(TestDocumentQueries.Create()));
}
