using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementDiagnostics.Diagnostics.GridNotPinned;
using Revit.Linter.ElementDiagnostics.Diagnostics.LevelNotPinned;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class PinnedDiagnosticsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unpinned_level_is_reported()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        var diagnostic = new LevelNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, level);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Pinned_level_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        Pin(document, level);
        var diagnostic = new LevelNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, level);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unpinned_grid_is_reported()
    {
        Document document = _document!;
        Grid grid = CreateGrid(document);
        var diagnostic = new GridNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, grid);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Pinned_grid_is_valid()
    {
        Document document = _document!;
        Grid grid = CreateGrid(document);
        Pin(document, grid);
        var diagnostic = new GridNotPinnedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, grid);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Grids_of_a_pinned_multi_segment_grid_are_not_reported()
    {
        Document document = _document!;
        MultiSegmentGrid chain = CreateMultiSegmentGrid(document);
        ElementId[] gridIds = [.. chain.GetGridIds()];
        await Assert.That(gridIds.Length).IsGreaterThan(0);
        Pin(document, chain);
        chain = (MultiSegmentGrid)document.GetElement(chain.Id)
            ?? throw new InvalidOperationException("The test multi-segment grid was not found.");
        var diagnostic = new GridNotPinnedDiagnostic();

        DiagnosticFeedback chainFeedback = diagnostic.Execute(document, null, chain);
        List<DiagnosticVerdict> segmentVerdicts = [];
        foreach (ElementId gridId in gridIds)
        {
            Element segment = document.GetElement(gridId)
                ?? throw new InvalidOperationException($"The multi-segment grid segment '{gridId}' was not found.");
            segmentVerdicts.Add(diagnostic.Execute(document, null, segment).Verdict);
        }

        await Assert.That(chainFeedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
        await Assert.That(segmentVerdicts.All(verdict => verdict == DiagnosticVerdict.Valid)).IsTrue();
    }

    [Test]
    public async Task Unpinned_multi_segment_grid_is_reported_once()
    {
        Document document = _document!;
        MultiSegmentGrid chain = CreateMultiSegmentGrid(document);
        ElementId[] gridIds = [.. chain.GetGridIds()];
        await Assert.That(gridIds.Length).IsGreaterThan(0);
        var diagnostic = new GridNotPinnedDiagnostic();

        DiagnosticFeedback chainFeedback = diagnostic.Execute(document, null, chain);
        List<DiagnosticVerdict> segmentVerdicts = [];
        foreach (ElementId gridId in gridIds)
        {
            Element segment = document.GetElement(gridId)
                ?? throw new InvalidOperationException($"The multi-segment grid segment '{gridId}' was not found.");
            segmentVerdicts.Add(diagnostic.Execute(document, null, segment).Verdict);
        }

        await Assert.That(chainFeedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(segmentVerdicts.All(verdict => verdict == DiagnosticVerdict.Valid)).IsTrue();
    }

    [Test]
    public async Task Level_filter_accepts_only_levels()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        Grid grid = CreateGrid(document);
        var filter = new LevelNotPinnedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, level)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, grid)).IsFalse();
    }

    [Test]
    public async Task Grid_filter_accepts_grids_and_multi_segment_grids()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        Grid grid = CreateGrid(document);
        MultiSegmentGrid chain = CreateMultiSegmentGrid(document);
        var filter = new GridNotPinnedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, grid)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, chain)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Both_checks_are_hidden_in_family_documents()
    {
        var levelFilter = new LevelNotPinnedDiagnosticDocumentFilter();
        var gridFilter = new GridNotPinnedDiagnosticDocumentFilter();
        Document projectDocument = _document!;

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(levelFilter.IsRelevantFor(projectDocument)).IsTrue();
            await Assert.That(gridFilter.IsRelevantFor(projectDocument)).IsTrue();
            await Assert.That(levelFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(gridFilter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    [Test]
    public async Task Pin_level_fix_pins_the_level()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        var fix = new PinLevel();

        bool result = ApplyFix(document, fix, level);

        Level pinnedLevel = (Level)document.GetElement(level.Id)
            ?? throw new InvalidOperationException("The test level was not found after the fix.");
        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0035");
        await Assert.That(result).IsTrue();
        await Assert.That(pinnedLevel.Pinned).IsTrue();
    }

    [Test]
    public async Task Pin_grid_fix_pins_the_grid()
    {
        Document document = _document!;
        Grid grid = CreateGrid(document);
        var fix = new PinGrid();

        bool result = ApplyFix(document, fix, grid);

        Grid pinnedGrid = (Grid)document.GetElement(grid.Id)
            ?? throw new InvalidOperationException("The test grid was not found after the fix.");
        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0036");
        await Assert.That(result).IsTrue();
        await Assert.That(pinnedGrid.Pinned).IsTrue();
    }

    [Test]
    public async Task Pin_grid_fix_pins_a_multi_segment_grid()
    {
        Document document = _document!;
        MultiSegmentGrid chain = CreateMultiSegmentGrid(document);
        var fix = new PinGrid();

        bool result = ApplyFix(document, fix, chain);

        MultiSegmentGrid pinnedChain = (MultiSegmentGrid)document.GetElement(chain.Id)
            ?? throw new InvalidOperationException("The test multi-segment grid was not found after the fix.");
        await Assert.That(result).IsTrue();
        await Assert.That(pinnedChain.Pinned).IsTrue();
    }

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }

    private static Grid CreateGrid(Document document)
    {
        using Transaction transaction = new(document, "Create test grid");
        transaction.Start();
        Grid grid = Grid.Create(document, Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 10000, 0)));
        transaction.Commit();
        return grid;
    }

    private static MultiSegmentGrid CreateMultiSegmentGrid(Document document)
    {
        using Transaction transaction = new(document, "Create test multi-segment grid");
        transaction.Start();
        SketchPlane sketchPlane = SketchPlane.Create(
            document, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero));
        GridType gridType = new FilteredElementCollector(document)
            .OfClass(typeof(GridType))
            .Cast<GridType>()
            .First();
        CurveLoop curveLoop = new();
        curveLoop.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10000, 0, 0)));
        curveLoop.Append(Line.CreateBound(new XYZ(10000, 0, 0), new XYZ(10000, 10000, 0)));
        curveLoop.Append(Line.CreateBound(new XYZ(10000, 10000, 0), new XYZ(0, 10000, 0)));
        ElementId chainId = MultiSegmentGrid.Create(document, gridType.Id, curveLoop, sketchPlane.Id);
        transaction.Commit();
        return (MultiSegmentGrid)document.GetElement(chainId)
            ?? throw new InvalidOperationException("The created multi-segment grid was not found.");
    }

    private static void Pin(Document document, Element element)
    {
        using Transaction transaction = new(document, "Pin test element");
        transaction.Start();
        element.Pinned = true;
        transaction.Commit();
    }

    private static bool ApplyFix(Document document, IElementFix fix, Element element)
    {
        using Transaction transaction = new(document, "Apply test fix");
        transaction.Start();
        bool result = fix.Execute(element);
        transaction.Commit();
        return result;
    }
}
