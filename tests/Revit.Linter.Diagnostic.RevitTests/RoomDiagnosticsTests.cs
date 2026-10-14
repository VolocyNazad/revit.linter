using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.RoomIsRedundant;
using Revit.Linter.ElementDiagnostics.Diagnostics.RoomNotEnclosed;
using Revit.Linter.ElementDiagnostics.Diagnostics.RoomUnplaced;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class RoomDiagnosticsTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unplaced_room_is_reported()
    {
        Document document = _document!;
        Phase phase = document.Phases.Cast<Phase>().FirstOrDefault()
            ?? throw new InvalidOperationException("The test project has no phases.");
        Room room;
        using (Transaction transaction = new(document, "Create test unplaced room"))
        {
            transaction.Start();
            room = document.Create.NewRoom(phase);
            transaction.Commit();
        }
        var diagnostic = new RoomUnplacedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, room);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Placed_room_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        CreateRoomEnclosure(document, level);
        Room room = PlaceRoom(document, level, new UV(5, 5));
        var diagnostic = new RoomUnplacedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, room);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unenclosed_room_is_reported()
    {
        // Placing a room without an enclosure is rolled back, so the unenclosed
        // room is an unplaced one: it has no boundaries by definition.
        Document document = _document!;
        Phase phase = document.Phases.Cast<Phase>().FirstOrDefault()
            ?? throw new InvalidOperationException("The test project has no phases.");
        Room room;
        using (Transaction transaction = new(document, "Create test unenclosed room"))
        {
            transaction.Start();
            room = document.Create.NewRoom(phase);
            transaction.Commit();
        }
        var diagnostic = new RoomNotEnclosedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, room);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Enclosed_room_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document);
        CreateRoomEnclosure(document, level);
        Room room = PlaceRoom(document, level, new UV(5, 5));
        var diagnostic = new RoomNotEnclosedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, room);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Only_rooms_are_checked_by_room_filters()
    {
        Document document = _document!;
        Phase phase = document.Phases.Cast<Phase>().FirstOrDefault()
            ?? throw new InvalidOperationException("The test project has no phases.");
        Level level = CreateLevel(document);
        Room room;
        using (Transaction transaction = new(document, "Create test room"))
        {
            transaction.Start();
            room = document.Create.NewRoom(phase);
            transaction.Commit();
        }
        var unplacedFilter = new RoomUnplacedDiagnosticFilter();
        var unenclosedFilter = new RoomNotEnclosedDiagnosticFilter();
        var redundantFilter = new RoomIsRedundantDiagnosticFilter();

        await Assert.That(unplacedFilter.IsRelevantFor(document, room)).IsTrue();
        await Assert.That(unenclosedFilter.IsRelevantFor(document, room)).IsTrue();
        await Assert.That(redundantFilter.IsRelevantFor(document, room)).IsTrue();
        await Assert.That(unplacedFilter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(unenclosedFilter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(redundantFilter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Room_checks_run_in_projects_only()
    {
        var unplacedFilter = new RoomUnplacedDiagnosticDocumentFilter();
        var unenclosedFilter = new RoomNotEnclosedDiagnosticDocumentFilter();
        var redundantFilter = new RoomIsRedundantDiagnosticDocumentFilter();
        await Assert.That(unplacedFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(unenclosedFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(redundantFilter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(unplacedFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(unenclosedFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(redundantFilter.IsRelevantFor(familyDocument)).IsFalse();
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static Level CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, 0);
        transaction.Commit();
        return level;
    }

    private static void CreateRoomEnclosure(Document document, Level level)
    {
        using Transaction transaction = new(document, "Create test room enclosure");
        transaction.Start();
        XYZ @base = new(0, 0, 0);
        Wall.Create(document, Line.CreateBound(@base, new XYZ(10, 0, 0)), level.Id, false);
        Wall.Create(document, Line.CreateBound(new XYZ(10, 0, 0), new XYZ(10, 10, 0)), level.Id, false);
        Wall.Create(document, Line.CreateBound(new XYZ(10, 10, 0), new XYZ(0, 10, 0)), level.Id, false);
        Wall.Create(document, Line.CreateBound(new XYZ(0, 10, 0), @base), level.Id, false);
        transaction.Commit();
    }

    private static Room PlaceRoom(Document document, Level level, UV point)
    {
        using Transaction transaction = new(document, "Place test room");
        transaction.Start();
        Room room = document.Create.NewRoom(level, point);
        transaction.Commit();
        return room;
    }
}
