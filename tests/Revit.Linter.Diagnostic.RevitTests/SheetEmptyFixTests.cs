using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.SheetEmpty;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class SheetEmptyFixTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Delete_empty_sheet_removes_sheet()
    {
        Document document = _document!;
        ViewSheet sheet;
        using (Transaction transaction = new(document, "Create test sheet"))
        {
            transaction.Start();
            sheet = ViewSheet.Create(document, ElementId.InvalidElementId);
            transaction.Commit();
        }
        ElementId sheetId = sheet.Id;
        var fix = new DeleteEmptySheet();

        bool result;
        using (Transaction transaction = new(document, "Delete test sheet"))
        {
            transaction.Start();
            result = fix.Execute(sheet);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0017");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(sheetId)).IsNull();
    }
}
