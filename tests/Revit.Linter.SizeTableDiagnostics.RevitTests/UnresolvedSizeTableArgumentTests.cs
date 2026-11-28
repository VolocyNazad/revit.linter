using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.SizeTableDiagnostics;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.UnresolvedSizeTableArgument;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.SizeTableDiagnostics.RevitTests;

public sealed class UnresolvedSizeTableArgumentTests : RevitApiTest
{
    private Document? _projectDocument;
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocuments()
    {
        _projectDocument = Application.NewProjectDocument(UnitSystem.Metric);
        _familyDocument = TestFamilyDocuments.OpenTemplateFamily(Application);
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
        var filter = new UnresolvedSizeTableArgumentDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(_projectDocument!)).IsFalse();
    }

    [Test]
    public async Task Expression_table_argument_is_reported()
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

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["argument"])
            .IsEqualTo(SizeTableDiagnosticLocalizations.GetString("DOC007_tableArgument"));
    }

    [Test]
    public async Task Expression_column_argument_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument,
            result,
            $"size_lookup(\"{tableName}\", if(Length > 0 mm, \"Length\", \"Width\"), Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["argument"])
            .IsEqualTo(SizeTableDiagnosticLocalizations.GetString("DOC007_columnArgument"));
    }

    [Test]
    public async Task Both_complex_arguments_are_reported_twice()
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
            $"size_lookup(if(Length > 0 mm, \"{firstTable}\", \"{secondTable}\"), if(Length > 0 mm, \"Length\", \"Width\"), Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(2);
        await Assert.That(feedback.Select(item =>
                (string)item.AdditionalMessageArguments!["argument"]))
            .Contains(SizeTableDiagnosticLocalizations.GetString("DOC007_tableArgument"));
        await Assert.That(feedback.Select(item =>
                (string)item.AdditionalMessageArguments!["argument"]))
            .Contains(SizeTableDiagnosticLocalizations.GetString("DOC007_columnArgument"));
    }

    [Test]
    public async Task Text_parameter_with_a_formula_is_resolved()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter table = SizeTableTestDocuments.AddTextParameter(familyDocument, "Table");
        SizeTableTestDocuments.SetFormula(
            familyDocument, table, $"if(Length > 0 mm, \"{tableName}\", \"Missing\")");
        FamilyParameter length = familyDocument.FamilyManager.GetParameters()
            .First(parameter => parameter.Definition.Name == "Length");
        SizeTableTestDocuments.SetLength(familyDocument, length, 1.0);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(Table, \"Length\", Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Simple_arguments_are_not_reported()
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

    private static UnresolvedSizeTableArgumentDiagnostic CreateDiagnostic() => new();
}
