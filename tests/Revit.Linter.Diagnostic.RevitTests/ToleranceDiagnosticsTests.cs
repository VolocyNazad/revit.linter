using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantCoordinates;
using Revit.Linter.ElementDiagnostics.Diagnostics.LocationLineElementWithTolerantLength;
using Revit.Linter.ElementDiagnostics.Diagnostics.LevelHeightIsTolerant;
using Revit.Linter.ElementDiagnostics.Diagnostics.WallHeightIsTolerant;
using Revit.Linter.ElementDiagnostics.Diagnostics.WallTopOffsetUnconnected;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ToleranceDiagnosticsTests : RevitApiTest
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
    public async Task Wall_without_top_constraint_is_reported()
    {
        Wall wall = CreateWall(_document!, 0, 3000);
        var diagnostic = new WallTopOffsetUnconnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Wall_with_top_constraint_is_valid()
    {
        Document document = _document!;
        Level lower = CreateLevel(document, 0);
        Level upper = CreateLevel(document, 3000);
        Wall wall = CreateWall(document, 0, 3000, lower);
        using (Transaction transaction = new(document, "Constrain test wall top"))
        {
            transaction.Start();
            wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(upper.Id);
            transaction.Commit();
        }
        var diagnostic = new WallTopOffsetUnconnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Wall_with_tolerant_height_is_reported()
    {
        Wall wall = CreateWall(_document!, 0, 3000.3);
        var diagnostic = new WallHeightIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Wall_with_exact_height_is_valid()
    {
        Wall wall = CreateWall(_document!, 0, 3000);
        var diagnostic = new WallHeightIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Level_with_tolerant_elevation_is_reported()
    {
        Level level = CreateLevel(_document!, 0.3);
        var diagnostic = new LevelHeightIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, level);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Level_with_exact_elevation_is_valid()
    {
        Level level = CreateLevel(_document!, 3000);
        var diagnostic = new LevelHeightIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, level);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Wall_with_tolerant_length_is_reported()
    {
        Wall wall = CreateOffsetWall(_document!, 0, 10.3);
        var diagnostic = new LocationLineElementWithTolerantLengthDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Wall_with_exact_length_is_valid()
    {
        Wall wall = CreateOffsetWall(_document!, 0, 10);
        var diagnostic = new LocationLineElementWithTolerantLengthDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Wall_with_tolerant_coordinates_is_reported()
    {
        Wall wall = CreateOffsetWall(_document!, 0.3, 10);
        var diagnostic = new LocationLineElementWithTolerantCoordinatesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Wall_with_exact_coordinates_is_valid()
    {
        Wall wall = CreateOffsetWall(_document!, 0, 10);
        var diagnostic = new LocationLineElementWithTolerantCoordinatesDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, wall);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_walls_are_checked_by_wall_filters()
    {
        Document document = _document!;
        Wall wall = CreateWall(document, 0, 3000);
        Level level = CreateLevel(document, 0);
        var heightFilter = new WallHeightIsTolerantDiagnosticFilter();
        var topFilter = new WallTopOffsetUnconnectedDiagnosticFilter();

        await Assert.That(heightFilter.IsRelevantFor(document, wall)).IsTrue();
        await Assert.That(topFilter.IsRelevantFor(document, wall)).IsTrue();
        await Assert.That(heightFilter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(topFilter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Only_levels_are_checked_by_level_filter()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        Wall wall = CreateWall(document, 0, 3000);
        var filter = new LevelHeightIsTolerantDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, level)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, wall)).IsFalse();
    }

    [Test]
    public async Task Only_line_based_elements_are_checked_by_location_filters()
    {
        Document document = _document!;
        Wall wall = CreateOffsetWall(document, 0, 10);
        Level level = CreateLevel(document, 0);
        var lengthFilter = new LocationLineElementWithTolerantLengthDiagnosticFilter();
        var coordinatesFilter = new LocationLineElementWithTolerantCoordinatesDiagnosticFilter();

        await Assert.That(lengthFilter.IsRelevantFor(document, wall)).IsTrue();
        await Assert.That(coordinatesFilter.IsRelevantFor(document, wall)).IsTrue();
        await Assert.That(lengthFilter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(coordinatesFilter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Tolerance_checks_run_in_projects_only()
    {
        var heightFilter = new WallHeightIsTolerantDiagnosticDocumentFilter();
        var topFilter = new WallTopOffsetUnconnectedDiagnosticDocumentFilter();
        var levelFilter = new LevelHeightIsTolerantDiagnosticDocumentFilter();
        var lengthFilter = new LocationLineElementWithTolerantLengthDiagnosticDocumentFilter();
        var coordinatesFilter = new LocationLineElementWithTolerantCoordinatesDiagnosticDocumentFilter();
        await Assert.That(heightFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(topFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(levelFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(lengthFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(coordinatesFilter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(heightFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(topFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(levelFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(lengthFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(coordinatesFilter.IsRelevantFor(familyDocument)).IsFalse();
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

    private static Wall CreateWall(Document document, double baseMillimeters, double heightMillimeters, Level? level = null)
    {
        level ??= CreateLevel(document, baseMillimeters);
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

    private static Wall CreateOffsetWall(Document document, double offsetMillimeters, double lengthMeters)
    {
        Level level = CreateLevel(document, 0);
        using Transaction transaction = new(document, "Create test offset wall");
        transaction.Start();
        Wall wall = Wall.Create(
            document,
            Line.CreateBound(
                new XYZ(offsetMillimeters * MmInFeet, 0, 0),
                new XYZ(offsetMillimeters * MmInFeet + lengthMeters, 0, 0)),
            level.Id,
            false);
        transaction.Commit();
        return wall;
    }
}
