using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.WallWithMultipleLayers;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class WallWithMultipleLayersDiagnosticsTests : RevitApiTest
{
    // Millimetres to Revit internal feet.
    private const double MmInFeet = 1.0 / 304.8;
    private const double WallLength = 10;
    private const double WallHeight = 3000;

    // Compound structure layers use internal feet as well.
    private const double LayerWidth = 0.2;

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Wall_with_multiple_layers_is_reported()
    {
        Wall wall = CreateWallWithLayers(_document!, 2);
        var diagnostic = new WallWithMultipleLayersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Wall_with_single_layer_is_valid()
    {
        Wall wall = CreateWallWithLayers(_document!, 1);
        var diagnostic = new WallWithMultipleLayersDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_walls_are_checked_by_wall_filter()
    {
        Document document = _document!;
        Wall wall = CreateWallWithLayers(document, 2);
        Level level = CreateLevel(document, 0);
        var filter = new WallWithMultipleLayersDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, wall)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Check_runs_in_projects_only()
    {
        var filter = new WallWithMultipleLayersDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static Level CreateLevel(Document document, double elevationMillimeters)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, elevationMillimeters * MmInFeet);
        transaction.Commit();
        return level;
    }

    private static Wall CreateWallWithLayers(Document document, int layerCount)
    {
        Level level = CreateLevel(document, 0);
        using Transaction transaction = new(document, "Create test wall");
        transaction.Start();
        WallType sourceType = new FilteredElementCollector(document)
            .OfClass(typeof(WallType))
            .Cast<WallType>()
            .First(wallType => wallType.Kind == WallKind.Basic);
        var wallType = (WallType)sourceType.Duplicate($"Linter test wall with {layerCount} layers");
        List<CompoundStructureLayer> layers = [];
        for (int index = 0; index < layerCount; index++)
        {
            layers.Add(new CompoundStructureLayer(
                LayerWidth,
                index == 0 ? MaterialFunctionAssignment.Structure : MaterialFunctionAssignment.Finish1,
                ElementId.InvalidElementId));
        }
        wallType.SetCompoundStructure(CompoundStructure.CreateSimpleCompoundStructure(layers));
        Wall wall = Wall.Create(
            document,
            Line.CreateBound(new XYZ(0, 0, 0), new XYZ(WallLength, 0, 0)),
            wallType.Id,
            level.Id,
            WallHeight * MmInFeet,
            0,
            false,
            false);
        transaction.Commit();
        return wall;
    }
}
