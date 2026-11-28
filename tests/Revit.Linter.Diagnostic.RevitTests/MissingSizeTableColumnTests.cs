using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.SizeTableDiagnostics.Diagnostics.MissingSizeTableColumn;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class MissingSizeTableColumnTests : RevitApiTest
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
        var filter = new MissingSizeTableColumnDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(_projectDocument!)).IsFalse();
    }

    [Test]
    public async Task Family_with_valid_column_reference_has_no_findings()
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
    public async Task Family_with_valid_header_parameter_has_no_findings()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter header = SizeTableTestDocuments.AddTextParameter(familyDocument, "Header");
        SizeTableTestDocuments.SetText(familyDocument, header, "Length");
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", Header, Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Formula_referencing_removed_column_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", \"Width\", Length, Length)");
        SizeTableTestDocuments.ImportTable(
            familyDocument, tableName, ",Length##length##millimeters\nS,100\n");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("Width");
        await Assert.That(feedback[0].MessageFormat).IsNull();
    }

    [Test]
    public async Task Header_parameter_with_missing_value_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter header = SizeTableTestDocuments.AddTextParameter(familyDocument, "Header");
        SizeTableTestDocuments.SetText(familyDocument, header, "Length");
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", Header, Width, Length)");
        SizeTableTestDocuments.SetText(familyDocument, header, "MissingHeader");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("MissingHeader");
        await Assert.That(feedback[0].MessageFormat).IsNull();
    }

    [Test]
    public async Task Parameter_driven_table_and_column_pairing_is_checked_per_type()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string firstTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.AddLengthParameter(familyDocument, "Depth");
        string secondTable = SizeTableTestDocuments.ImportTable(
            familyDocument, ",Length##length##millimeters,Depth##length##millimeters\nS,100,50\n");
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyManager familyManager = familyDocument.FamilyManager;
        FamilyType firstType = familyManager.CurrentType!;
        FamilyType secondType = SizeTableTestDocuments.AddType(familyDocument, "SecondType");
        FamilyParameter tableName = SizeTableTestDocuments.AddTextParameter(familyDocument, "TableName");
        FamilyParameter header = SizeTableTestDocuments.AddTextParameter(familyDocument, "Header");
        SizeTableTestDocuments.SetText(familyDocument, firstType, tableName, firstTable);
        SizeTableTestDocuments.SetText(familyDocument, firstType, header, "Width");
        SizeTableTestDocuments.SetText(familyDocument, secondType, tableName, secondTable);
        SizeTableTestDocuments.SetText(familyDocument, secondType, header, "Depth");
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(TableName, Header, Length, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Parameter_driven_missing_column_of_one_type_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string firstTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.AddLengthParameter(familyDocument, "Depth");
        string secondTable = SizeTableTestDocuments.ImportTable(
            familyDocument, ",Length##length##millimeters,Depth##length##millimeters\nS,100,50\n");
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyManager familyManager = familyDocument.FamilyManager;
        FamilyType firstType = familyManager.CurrentType!;
        FamilyType secondType = SizeTableTestDocuments.AddType(familyDocument, "SecondType");
        FamilyParameter tableName = SizeTableTestDocuments.AddTextParameter(familyDocument, "TableName");
        FamilyParameter header = SizeTableTestDocuments.AddTextParameter(familyDocument, "Header");
        SizeTableTestDocuments.SetText(familyDocument, firstType, tableName, firstTable);
        SizeTableTestDocuments.SetText(familyDocument, firstType, header, "Width");
        SizeTableTestDocuments.SetText(familyDocument, secondType, tableName, secondTable);
        SizeTableTestDocuments.SetText(familyDocument, secondType, header, "Depth");
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(TableName, Header, Length, Length)");
        SizeTableTestDocuments.ImportTable(
            familyDocument, secondTable, ",Length##length##millimeters\nS,100\n");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["parameterName"]).IsEqualTo("L");
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(secondTable);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("Depth");
    }

    [Test]
    public async Task Instance_column_parameter_with_valid_default_has_no_findings()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter header = SizeTableTestDocuments.AddInstanceTextParameter(familyDocument, "HeaderInstance");
        SizeTableTestDocuments.SetText(familyDocument, header, "Length");
        FamilyParameter result = SizeTableTestDocuments.AddInstanceLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", HeaderInstance, Width, Length)");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback).IsEmpty();
    }

    [Test]
    public async Task Instance_column_parameter_with_missing_default_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyParameter header = SizeTableTestDocuments.AddInstanceTextParameter(familyDocument, "HeaderInstance");
        SizeTableTestDocuments.SetText(familyDocument, header, "Length");
        FamilyParameter result = SizeTableTestDocuments.AddInstanceLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", HeaderInstance, Width, Length)");
        SizeTableTestDocuments.SetText(familyDocument, header, "Missing");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("Missing");
    }

    [Test]
    public async Task Literal_table_with_missing_column_of_one_type_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string tableName = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyManager familyManager = familyDocument.FamilyManager;
        FamilyType firstType = familyManager.CurrentType!;
        FamilyType secondType = SizeTableTestDocuments.AddType(familyDocument, "SecondType");
        FamilyParameter header = SizeTableTestDocuments.AddTextParameter(familyDocument, "Header");
        SizeTableTestDocuments.SetText(familyDocument, firstType, header, "Width");
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, $"size_lookup(\"{tableName}\", Header, Width, Length)");
        SizeTableTestDocuments.SetText(familyDocument, secondType, header, "Depth");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(tableName);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("Depth");
    }

    [Test]
    public async Task Parameter_table_with_missing_literal_column_of_one_type_is_reported()
    {
        await Assert.That(_familyDocument).IsNotNull();
        Document familyDocument = _familyDocument!;
        string firstTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        string secondTable = SizeTableTestDocuments.ImportTable(
            familyDocument, SizeTableTestDocuments.ProbeTableCsv);
        SizeTableTestDocuments.EnsureCurrentType(familyDocument);
        FamilyManager familyManager = familyDocument.FamilyManager;
        FamilyType firstType = familyManager.CurrentType!;
        FamilyType secondType = SizeTableTestDocuments.AddType(familyDocument, "SecondType");
        FamilyParameter table = SizeTableTestDocuments.AddTextParameter(familyDocument, "TableName");
        SizeTableTestDocuments.SetText(familyDocument, firstType, table, firstTable);
        SizeTableTestDocuments.SetText(familyDocument, secondType, table, secondTable);
        FamilyParameter result = SizeTableTestDocuments.AddLengthParameter(familyDocument, "L");
        SizeTableTestDocuments.SetFormula(
            familyDocument, result, "size_lookup(TableName, \"Width\", Length, Length)");
        SizeTableTestDocuments.ImportTable(
            familyDocument, secondTable, ",Length##length##millimeters\nS,100\n");

        IReadOnlyList<DiagnosticFeedback> feedback = CreateDiagnostic().Execute(familyDocument).ToArray();

        await Assert.That(feedback.Count()).IsEqualTo(1);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["tableName"]).IsEqualTo(secondTable);
        await Assert.That((string)feedback[0].AdditionalMessageArguments!["columnName"]).IsEqualTo("Width");
    }

    private static MissingSizeTableColumnDiagnostic CreateDiagnostic() =>
        new();
}
