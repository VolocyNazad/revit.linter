using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.SheetWithMultipleTitleBlocks;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class TitleBlockFixTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Keep_first_title_block_deletes_extras()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        FamilyTestDocuments.PlaceTitleBlocks(document, sheet, 2);
        var fix = new KeepFirstTitleBlock();

        bool result;
        using (Transaction transaction = new(document, "Delete extra title blocks"))
        {
            transaction.Start();
            result = fix.Execute(sheet);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0020");
        await Assert.That(result).IsTrue();
        await Assert.That(CountTitleBlocks(document)).IsEqualTo(1);
    }

    [Test]
    public async Task Single_title_block_is_kept()
    {
        Document document = _document!;
        ViewSheet sheet = CreateSheet(document);
        FamilyTestDocuments.PlaceTitleBlocks(document, sheet, 1);
        var fix = new KeepFirstTitleBlock();

        bool result;
        using (Transaction transaction = new(document, "Delete extra title blocks"))
        {
            transaction.Start();
            result = fix.Execute(sheet);
            transaction.Commit();
        }

        await Assert.That(result).IsFalse();
        await Assert.That(CountTitleBlocks(document)).IsEqualTo(1);
    }

    private static ViewSheet CreateSheet(Document document)
    {
        using Transaction transaction = new(document, "Create test sheet");
        transaction.Start();
        ViewSheet sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
        transaction.Commit();
        return sheet;
    }

    private static int CountTitleBlocks(Document document) =>
        new FilteredElementCollector(document)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsNotElementType()
            .GetElementCount();
}
