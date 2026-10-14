using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.ReferencePlaneUnused;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ReferencePlaneFixTests : RevitApiTest
{
    private Document? _familyDocument;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() =>
        _familyDocument = FamilyTestDocuments.OpenTemplateFamily(Application);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _familyDocument?.Close(false);

    [Test]
    public async Task Delete_unused_plane_removes_plane()
    {
        Document document = _familyDocument!;
        ViewPlan view = new FilteredElementCollector(document)
            .OfClass(typeof(ViewPlan))
            .Cast<ViewPlan>()
            .FirstOrDefault()
            ?? throw new InvalidOperationException("The test family has no plan view.");
        ReferencePlane plane;
        using (Transaction transaction = new(document, "Create test reference plane"))
        {
            transaction.Start();
            plane = document.FamilyCreate.NewReferencePlane(
                new XYZ(0, 0, 0), new XYZ(0, 10, 0), XYZ.BasisZ, view);
            transaction.Commit();
        }
        ElementId planeId = plane.Id;
        var fix = new DeleteUnusedReferencePlane();

        bool result;
        using (Transaction transaction = new(document, "Delete test reference plane"))
        {
            transaction.Start();
            result = fix.Execute(plane);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0024");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(planeId)).IsNull();
    }
}
