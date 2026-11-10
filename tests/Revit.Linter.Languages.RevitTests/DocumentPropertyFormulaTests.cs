using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;

namespace Revit.Linter.Languages.RevitTests;

public sealed class DocumentPropertyFormulaTests : RevitApiTest
{
    private static Document? _document;

    [Before(Class)]
    public static void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Class)]
    public static void CloseDocument()
    {
        _document?.Close(false);
        _document = null;
    }

    [Test]
    public async Task Title_property_is_available()
    {
        Func<Document, bool> formula =
            PropertyFormulaCompiler.Compile<Document, bool>("!isnullorempty(property('Title'))");

        bool result = formula.Invoke(_document!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Parameterless_document_method_is_available()
    {
        Func<Document, bool> formula =
            PropertyFormulaCompiler.Compile<Document, bool>("!isnull(method('GetWarnings'))");

        bool result = formula.Invoke(_document!);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Dynamically_selected_document_method_is_available()
    {
        Func<Document, bool> formula = PropertyFormulaCompiler.Compile<Document, bool>(
            "!isnull(method(if(true, 'GetWarnings', 'Missing')))");

        bool result = formula.Invoke(_document!);

        await Assert.That(result).IsTrue();
    }
}
