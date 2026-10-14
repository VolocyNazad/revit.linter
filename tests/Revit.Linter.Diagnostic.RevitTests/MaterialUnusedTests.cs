using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.MaterialUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class MaterialUnusedTests : RevitApiTest
{
    // Millimetres to Revit internal feet.
    private const double MmInFeet = 1.0 / 304.8;

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unused_material_is_reported()
    {
        Document document = _document!;
        Material material = CreateMaterial(document, "LinterUnusedMaterial");
        // The test query cache never invalidates: the diagnostic is created after every
        // document change so the used-material set is built over the final state.
        var diagnostic = new MaterialUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Geometry_used_material_is_valid()
    {
        Document document = _document!;
        Material material = CreateMaterial(document, "LinterGeometryMaterial");
        Wall wall = CreateWall(document, 0, 3000);
        AssignSingleLayerMaterial(document, wall, material.Id);
        var diagnostic = new MaterialUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Paint_only_material_is_valid()
    {
        // GetMaterialIds(false) sees geometry and compound materials while GetMaterialIds(true)
        // sees painted faces: the sets are disjoint, so a paint-only material must also count as used.
        Document document = _document!;
        Material material = CreateMaterial(document, "LinterPaintMaterial");
        Wall wall = CreateWall(document, 0, 3000);
        PaintFirstSolidFace(document, wall, material.Id);
        var diagnostic = new MaterialUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    private static Material CreateMaterial(Document document, string name)
    {
        using Transaction transaction = new(document, "Create test material");
        transaction.Start();
        ElementId materialId = Material.Create(document, name);
        transaction.Commit();
        return (Material)document.GetElement(materialId);
    }

    private static Level CreateLevel(Document document, double elevationMillimeters)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, elevationMillimeters * MmInFeet);
        transaction.Commit();
        return level;
    }

    private static Wall CreateWall(Document document, double baseMillimeters, double heightMillimeters)
    {
        Level level = CreateLevel(document, baseMillimeters);
        using Transaction transaction = new(document, "Create test wall");
        transaction.Start();
        Wall wall = Wall.Create(
            document,
            Line.CreateBound(new XYZ(0, 0, baseMillimeters * MmInFeet), new XYZ(10, 0, baseMillimeters * MmInFeet)),
            level.Id,
            false);
        wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).Set(heightMillimeters * MmInFeet);
        transaction.Commit();
        return wall;
    }

    private static void AssignSingleLayerMaterial(Document document, Wall wall, ElementId materialId)
    {
        using Transaction transaction = new(document, "Assign test wall material");
        transaction.Start();
        WallType wallType = (WallType)document.GetElement(wall.GetTypeId());
        CompoundStructure structure = CompoundStructure.CreateSingleLayerCompoundStructure(
            MaterialFunctionAssignment.Structure, 200 * MmInFeet, materialId);
        wallType.SetCompoundStructure(structure);
        transaction.Commit();
    }

    private static void PaintFirstSolidFace(Document document, Wall wall, ElementId materialId)
    {
        Options options = new() { ComputeReferences = true };
        Face? face = wall.get_Geometry(options)?
            .OfType<Solid>()
            .Where(solid => solid.Volume > 0)
            .SelectMany(solid => solid.Faces.OfType<Face>())
            .FirstOrDefault();
        if (face is null)
            throw new InvalidOperationException("Test wall has no solid face to paint.");
        using Transaction transaction = new(document, "Paint test wall face");
        transaction.Start();
        document.Paint(wall.Id, face, materialId);
        transaction.Commit();
    }
}
