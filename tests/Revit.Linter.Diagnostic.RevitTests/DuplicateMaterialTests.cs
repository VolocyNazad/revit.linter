using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class DuplicateMaterialTests : RevitApiTest
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
    public async Task Identical_materials_are_reported()
    {
        Document document = _document!;
        Material first = CreateMaterial(document, "LinterDuplicateMaterialA");
        Material second = CreateMaterial(document, "LinterDuplicateMaterialB");
        Material canonical = first.Id < second.Id ? first : second;
        Material duplicate = first.Id < second.Id ? second : first;
        // The test query cache never invalidates: the diagnostic is created after every document
        // change so the signatures are built over the final state.
        var diagnostic = new DuplicateMaterialDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback canonicalFeedback = diagnostic.Execute(document, null, canonical);
        DiagnosticFeedback duplicateFeedback = diagnostic.Execute(document, null, duplicate);

        await Assert.That(canonicalFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(duplicateFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That((string)duplicateFeedback.AdditionalMessageArguments!["duplicateName"])
            .IsEqualTo(canonical.Name);
    }

    [Test]
    public async Task Diverged_materials_are_valid()
    {
        Document document = _document!;
        Material first = CreateMaterial(document, "LinterTransparencyMaterialA");
        Material second = CreateMaterial(document, "LinterTransparencyMaterialB");
        SetTransparency(document, second, 50);
        var diagnostic = new DuplicateMaterialDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Diverged_color_makes_materials_valid()
    {
        // Color is part of the compared settings, so changing only it must separate
        // materials that were duplicates before.
        Document document = _document!;
        Material first = CreateMaterial(document, "LinterColorMaterialA");
        Material second = CreateMaterial(document, "LinterColorMaterialB");
        SetColor(document, second, new Color(10, 20, 30));
        var diagnostic = new DuplicateMaterialDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback firstFeedback = diagnostic.Execute(document, null, first);
        DiagnosticFeedback secondFeedback = diagnostic.Execute(document, null, second);

        await Assert.That(firstFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(secondFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unused_duplicate_fix_deletes_material()
    {
        Document document = _document!;
        Material first = CreateMaterial(document, "LinterFixDuplicateMaterialA");
        Material second = CreateMaterial(document, "LinterFixDuplicateMaterialB");
        Material duplicate = first.Id < second.Id ? second : first;
        ElementId duplicateId = duplicate.Id;
        var fix = new DeleteDuplicateMaterial(TestDocumentQueries.Create());

        bool result;
        using (Transaction transaction = new(document, "Delete test duplicate material"))
        {
            transaction.Start();
            result = fix.Execute(duplicate);
            transaction.Commit();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(duplicateId)).IsNull();
    }

    [Test]
    public async Task Used_duplicate_fix_keeps_material()
    {
        // Deleting a material that geometry still references would break the model, so the fix
        // must refuse the used copy instead of removing it.
        Document document = _document!;
        Material first = CreateMaterial(document, "LinterUsedDuplicateMaterialA");
        Material second = CreateMaterial(document, "LinterUsedDuplicateMaterialB");
        Material duplicate = first.Id < second.Id ? second : first;
        Wall wall = CreateWall(document, 0, 3000);
        AssignSingleLayerMaterial(document, wall, duplicate.Id);
        var fix = new DeleteDuplicateMaterial(TestDocumentQueries.Create());

        bool result;
        using (Transaction transaction = new(document, "Try deleting used duplicate material"))
        {
            transaction.Start();
            result = fix.Execute(duplicate);
            transaction.Commit();
        }

        await Assert.That(result).IsFalse();
        await Assert.That(document.GetElement(duplicate.Id)).IsNotNull();
    }

    private static Material CreateMaterial(Document document, string name)
    {
        using Transaction transaction = new(document, "Create test material");
        transaction.Start();
        ElementId materialId = Material.Create(document, name);
        transaction.Commit();
        return (Material)document.GetElement(materialId);
    }

    private static void SetTransparency(Document document, Material material, int transparency)
    {
        using Transaction transaction = new(document, "Diverge test material");
        transaction.Start();
        material.Transparency = transparency;
        transaction.Commit();
    }

    private static void SetColor(Document document, Material material, Color color)
    {
        using Transaction transaction = new(document, "Diverge test material color");
        transaction.Start();
        material.Color = color;
        transaction.Commit();
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
}
