using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferenceLineUnused;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ReferenceLineFixTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() =>
        _familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _familyDocument?.Close(false);

    [Test]
    public async Task Delete_unused_line_removes_line()
    {
        Document document = _familyDocument!;
        ModelCurve line;
        using (Transaction transaction = new(document, "Create test reference line"))
        {
            transaction.Start();
            line = CreateReferenceLine(document, new XYZ(0, 0, 0), new XYZ(0, 10, 0));
            transaction.Commit();
        }
        ElementId lineId = line.Id;
        var fix = new DeleteUnusedReferenceLine();

        bool result;
        using (Transaction transaction = new(document, "Delete test reference line"))
        {
            transaction.Start();
            result = fix.Execute(line);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0029");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(lineId)).IsNull();
    }

    private static ModelCurve CreateReferenceLine(Document document, XYZ start, XYZ end)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketchPlane = SketchPlane.Create(document, plane);
        ModelCurve line = document.FamilyCreate.NewModelCurve(Line.CreateBound(start, end), sketchPlane);
        line.ChangeToReferenceLine();
        return line;
    }
}
