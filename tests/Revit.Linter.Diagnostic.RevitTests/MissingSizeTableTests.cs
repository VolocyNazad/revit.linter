using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.DocumentDiagnostics.Diagnostics.MissingSizeTable;
using Revit.Linter.DocumentDiagnostics.Diagnostics.SizeTables;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class MissingSizeTableTests : RevitApiTest
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
        var filter = new MissingSizeTableDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(_projectDocument!)).IsFalse();
    }

    [Test]
    public async Task Family_with_valid_table_reference_has_no_findings()
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
    public async Task Formula_referencing_removed_table_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", \"Length\", Width, Length)");
        SizeTableTestDocuments.RemoveTable(familyDocument, tableName);

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That(feedback[0].Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        await Assert.That(feedback[0].MessageFormat).IsNull();
    }

    private static MissingSizeTableDiagnostic CreateDiagnostic() =>
        new(new SizeTableReferenceCollector(TestDocumentQueries.Create()));
}
