using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.PurgeableElementUnused;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class PurgeableElementTests : RevitApiTest
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
    public async Task Duplicated_wall_type_is_reported()
    {
        Document document = _document!;
        Wall wall = CreateWall(document, 0, 3000);
        WallType duplicate = DuplicateWallType(document, wall.WallType);
        var diagnostic = new PurgeableElementUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, duplicate);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Used_wall_type_is_valid()
    {
        Document document = _document!;
        Wall wall = CreateWall(document, 0, 3000);
        var diagnostic = new PurgeableElementUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, wall.WallType);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    // Families and their types are purgeable too: RVT003 reports them even though FamilyUnused
    // and FamilyTypeUnused have dedicated diagnostics, so the overlap is intentional.
    [Test]
    public async Task Unused_family_type_is_reported()
    {
        Document document = _document!;
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = (FamilySymbol)document.GetElement(family.GetFamilySymbolIds().First());
        var diagnostic = new PurgeableElementUnusedDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, symbol);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Filter_does_not_exclude_family_elements()
    {
        Document document = _document!;
        Wall wall = CreateWall(document, 0, 3000);
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        var filter = new PurgeableElementUnusedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, wall.WallType)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, family)).IsTrue();
    }

    [Test]
    public async Task Check_runs_in_any_document()
    {
        var filter = new PurgeableElementUnusedDiagnosticDocumentFilter();
        await Assert.That(filter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(filter.IsRelevantFor(familyDocument)).IsTrue();
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

    private static WallType DuplicateWallType(Document document, WallType source)
    {
        using Transaction transaction = new(document, "Duplicate test wall type");
        transaction.Start();
        WallType duplicate = (WallType)source.Duplicate("LinterUnusedType");
        transaction.Commit();
        return duplicate;
    }
}
