using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class FamilyFormulaRankTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument()
    {
        _document = FamilyTestDocuments.OpenTemplateFamily(Application);
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument()
    {
        _document?.Close(false);
    }

    [Test]
    public async Task Type_formula_cannot_reference_an_instance_parameter()
    {
        Document document = _document!;
        SizeTableTestDocuments.EnsureCurrentType(document);
        FamilyParameter type = SizeTableTestDocuments.AddLengthParameter(document, "Type");
        SizeTableTestDocuments.AddInstanceLengthParameter(document, "Instance");

        Assert.Throws<Autodesk.Revit.Exceptions.InvalidOperationException>(() =>
            SizeTableTestDocuments.SetFormula(document, type, "Instance"));
    }

    [Test]
    public async Task Instance_formula_can_reference_a_type_parameter()
    {
        Document document = _document!;
        SizeTableTestDocuments.EnsureCurrentType(document);
        SizeTableTestDocuments.AddLengthParameter(document, "Type");
        FamilyParameter instance = SizeTableTestDocuments.AddInstanceLengthParameter(document, "Instance");

        SizeTableTestDocuments.SetFormula(document, instance, "Type");
    }

    [Test]
    public async Task Instance_formula_can_reference_an_instance_parameter()
    {
        Document document = _document!;
        SizeTableTestDocuments.EnsureCurrentType(document);
        SizeTableTestDocuments.AddInstanceLengthParameter(document, "Source");
        FamilyParameter target = SizeTableTestDocuments.AddInstanceLengthParameter(document, "Target");

        SizeTableTestDocuments.SetFormula(document, target, "Source");
    }

    [Test]
    public async Task Type_formula_can_reference_a_type_parameter()
    {
        Document document = _document!;
        SizeTableTestDocuments.EnsureCurrentType(document);
        SizeTableTestDocuments.AddLengthParameter(document, "Source");
        FamilyParameter target = SizeTableTestDocuments.AddLengthParameter(document, "Target");

        SizeTableTestDocuments.SetFormula(document, target, "Source");
    }
}
