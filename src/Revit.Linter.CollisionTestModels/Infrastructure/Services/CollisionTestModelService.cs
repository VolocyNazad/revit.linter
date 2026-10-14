using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Revit.Linter.CollisionTestModels.Abstractions.Models;
using Revit.Linter.CollisionTestModels.Abstractions.Services;

namespace Revit.Linter.CollisionTestModels.Infrastructure.Services;

/// <summary>
/// Creates a host document with two shared-coordinates links for collision-diagnostic testing.
/// </summary>
/// <remarks>
/// Layout in host coordinates (feet), covering these scenarios:
/// <list type="bullet">
/// <item>Host walls H1 (along X, 0..10) and H2 (across, X=5, -5..5) cross each other:
/// Revit joins crossing walls, so they are expected to only touch and host-versus-host
/// stays silent.</item>
/// <item>Host wall H5 (along X, 60..70) touches H4 (along X, 50..60) end to end:
/// the touch-only control, expected to stay silent.</item>
/// <item>Host wall H6 (across, X=80, Y 0..10) stands clear of every host joint, so its solid
/// keeps full volume along its whole length.</item>
/// <item>Host floor F1 (0..10 by 0..8) crosses H1 and H2 in plan but only touches them
/// in height (wall bases sit on the slab top), so wall-versus-floor stays silent.
/// Floors never overlap each other: Revit rolls overlapping floor sketches back
/// without posting a failure, so volumetric host-versus-host coverage stays on
/// the system runs below.</item>
/// <item>Host wall H4 (along X, 50..60) is touched end to end by H5 and by the link B
/// touch wall but overlaps nothing: the wall touch-only control.</item>
/// <item>Host floor F2 (100..110 by 100..108) is isolated:
/// the valid (no-collision) control.</item>
/// <item>Link A lands on 0..10 and hits H1, H2 and F1 (its floor lands exactly on F1):
/// host-versus-link collisions.</item>
/// <item>Link B lands on 30..40 and hits H3 (30..40): host-versus-link collisions on another target.</item>
/// <item>Link B also carries a wall landing on 45..50 that only touches H4, a wall landing
/// across H6 (75..85 at Y=5) for a joint-free crossing hit, and an isolated
/// floor: the link-side touch-only and no-collision controls.</item>
/// <item>Host duct D1 (along X, 0..10 at Y=2, Z=9) is crossed by host pipe P1
/// (across, X=5, Y -2..6 at Z=9): the host-versus-host system collision. A host cable tray T1
/// runs coincident with D1 and hits it as well as P1.</item>
/// <item>Host duct D2 with host pipe P2, both along X, 30..40 at Y=2, Z=9, run coincident:
/// another host-versus-host system collision on a second target.</item>
/// <item>Host duct D3 (along X, 100..110 at Y=102, Z=9) with host tray T3 two feet away
/// are isolated: the system no-collision controls.</item>
/// <item>Link A carries a duct and a pipe landing on the D1 run: host-versus-link system
/// collisions for both duct and tray targets.</item>
/// <item>Link B carries a pipe landing on the D2 run: host-versus-link system collisions
/// on another target.</item>
/// </list>
/// Walls cross instead of overlapping collinearly so no room-boundary warning is posted.
/// Both links land through a genuine shared site with a non-identity <c>GetTotalTransform</c>.
/// All three files are saved on disk and can be reopened.
/// <para>The shared site is established by file lineage: both links and the host are saved from
/// one document, so they carry the same site record. Only the host document is ever modified
/// after linking, because transactions are limited to primary documents.</para>
/// <para>Runs need their system and type objects from the new project document (a floor type,
/// duct and piping system types with duct, pipe and tray types); a missing kind fails
/// the creation with a describing error instead of building a partial model silently.</para>
/// </remarks>
internal sealed class CollisionTestModelService(ILogger<CollisionTestModelService>? logger = null)
    : ICollisionTestModelService
{
    private static readonly XYZ HostWall1Start = new(0, 0, 0);
    private static readonly XYZ HostWall1End = new(10, 0, 0);
    private static readonly XYZ HostWall2Start = new(5, -5, 0);
    private static readonly XYZ HostWall2End = new(5, 5, 0);
    private static readonly XYZ HostWall3Start = new(30, 0, 0);
    private static readonly XYZ HostWall3End = new(40, 0, 0);
    private static readonly XYZ HostWall4Start = new(50, 0, 0);
    private static readonly XYZ HostWall4End = new(60, 0, 0);
    private static readonly XYZ HostWall5Start = new(60, 0, 0);
    private static readonly XYZ HostWall5End = new(70, 0, 0);
    private static readonly XYZ HostWall6Start = new(80, 0, 0);
    private static readonly XYZ HostWall6End = new(80, 10, 0);
    private static readonly XYZ LinkAWallStart = new(-5, 0, 0);
    private static readonly XYZ LinkAWallEnd = new(5, 0, 0);
    private static readonly XYZ LinkBWallStart = new(25, 0, 0);
    private static readonly XYZ LinkBWallEnd = new(35, 0, 0);
    private static readonly XYZ LinkBTouchWallStart = new(40, 0, 0);
    private static readonly XYZ LinkBTouchWallEnd = new(45, 0, 0);
    private static readonly XYZ LinkBCrossWallStart = new(70, 5, 0);
    private static readonly XYZ LinkBCrossWallEnd = new(80, 5, 0);

    // Floor rectangles in plan, feet: host content in host coordinates, link content in link
    // coordinates (link east plus five feet is host east through the shared site).
    private static readonly (double MinX, double MinY, double MaxX, double MaxY) HostFloor1 = (0, 0, 10, 8);
    private static readonly (double MinX, double MinY, double MaxX, double MaxY) HostFloor2 = (100, 100, 110, 108);
    private static readonly (double MinX, double MinY, double MaxX, double MaxY) LinkAFloor = (-5, 0, 5, 8);
    private static readonly (double MinX, double MinY, double MaxX, double MaxY) LinkBFloor = (200, 200, 210, 208);

    // Ventilation, piping and tray runs: host content in host coordinates, link content in link
    // coordinates (link east plus five feet is host east through the shared site). Runs sit at
    // Z=9 feet: clear of the floor slabs below and inside the wall height, which the collision
    // rules under test never pair with systems.
    private static readonly XYZ HostDuct1Start = new(0, 2, 9);
    private static readonly XYZ HostDuct1End = new(10, 2, 9);
    private static readonly XYZ HostPipe1Start = new(5, -2, 9);
    private static readonly XYZ HostPipe1End = new(5, 6, 9);
    private static readonly XYZ HostTray1Start = new(0, 2, 9);
    private static readonly XYZ HostTray1End = new(10, 2, 9);
    private static readonly XYZ HostDuct2Start = new(30, 2, 9);
    private static readonly XYZ HostDuct2End = new(40, 2, 9);
    private static readonly XYZ HostPipe2Start = new(30, 2, 9);
    private static readonly XYZ HostPipe2End = new(40, 2, 9);
    private static readonly XYZ HostDuct3Start = new(100, 102, 9);
    private static readonly XYZ HostDuct3End = new(110, 102, 9);
    private static readonly XYZ HostTray3Start = new(100, 104, 9);
    private static readonly XYZ HostTray3End = new(110, 104, 9);
    private static readonly XYZ LinkADuctStart = new(-5, 2, 9);
    private static readonly XYZ LinkADuctEnd = new(5, 2, 9);
    private static readonly XYZ LinkAPipeStart = new(-5, 2, 9);
    private static readonly XYZ LinkAPipeEnd = new(5, 2, 9);
    private static readonly XYZ LinkBPipeStart = new(25, 2, 9);
    private static readonly XYZ LinkBPipeEnd = new(35, 2, 9);
    private const double HostSharedEast = -5.0;

    private readonly ILogger<CollisionTestModelService> _logger = logger ?? NullLogger<CollisionTestModelService>.Instance;

    /// <inheritdoc />
    public CollisionTestModelResult CreateHostWithTwoLinks(Autodesk.Revit.ApplicationServices.Application application)
    {
        if (application is null) throw new ArgumentNullException(nameof(application));

        // Each run gets its own directory: reruns never clash with documents that earlier runs
        // left open, and Revit never reports the links as touched by another host in this session.
        string directory = Path.Combine(
            Path.GetTempPath(),
            "RevitLinterCollisionTest",
            $"{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string linkAPath = Path.Combine(directory, "LinterCollisionLinkA.rvt");
        string linkBPath = Path.Combine(directory, "LinterCollisionLinkB.rvt");
        string hostPath = Path.Combine(directory, "LinterCollisionHost.rvt");

        // The link files and the host share one lineage, hence one shared site.
        // Link A is drawn at -5..5 and lands on 0..10; link B is drawn at 25..35 and lands on 30..40.
        Document hostDocument = application.NewProjectDocument(UnitSystem.Imperial);
        ElementId levelId = CreateLevel(hostDocument);
        Level level = (Level)hostDocument.GetElement(levelId);
        FloorType floorType = GetDefaultFloorType(hostDocument);
        DuctType ductType = GetFirstType<DuctType>(hostDocument, "duct types");
        MechanicalSystemType ductSystemType = GetFirstType<MechanicalSystemType>(hostDocument, "duct system types");
        PipeType pipeType = GetFirstType<PipeType>(hostDocument, "pipe types");
        PipingSystemType pipeSystemType = GetFirstType<PipingSystemType>(hostDocument, "piping system types");
        CableTrayType trayType = GetFirstType<CableTrayType>(hostDocument, "cable tray types");
        ElementId linkAWallId = CreateWall(hostDocument, levelId, LinkAWallStart, LinkAWallEnd);
        ElementId linkAFloorId = CreateFloor(hostDocument, level, floorType, LinkAFloor);
        ElementId linkADuctId = CreateDuct(hostDocument, ductSystemType.Id, ductType.Id, levelId, LinkADuctStart, LinkADuctEnd);
        ElementId linkAPipeId = CreatePipe(hostDocument, pipeSystemType.Id, pipeType.Id, levelId, LinkAPipeStart, LinkAPipeEnd);
        SaveDocument(hostDocument, linkAPath);
        DeleteElements(hostDocument, [linkAWallId, linkAFloorId, linkADuctId, linkAPipeId]);
        ElementId linkBWallId = CreateWall(hostDocument, levelId, LinkBWallStart, LinkBWallEnd);
        ElementId linkBTouchWallId = CreateWall(hostDocument, levelId, LinkBTouchWallStart, LinkBTouchWallEnd);
        ElementId linkBCrossWallId = CreateWall(hostDocument, levelId, LinkBCrossWallStart, LinkBCrossWallEnd);
        ElementId linkBFloorId = CreateFloor(hostDocument, level, floorType, LinkBFloor);
        ElementId linkBPipeId = CreatePipe(hostDocument, pipeSystemType.Id, pipeType.Id, levelId, LinkBPipeStart, LinkBPipeEnd);
        SaveDocument(hostDocument, linkBPath);
        DeleteElements(hostDocument, [linkBWallId, linkBTouchWallId, linkBCrossWallId, linkBFloorId, linkBPipeId]);
        CreateWall(hostDocument, levelId, HostWall1Start, HostWall1End);
        CreateWall(hostDocument, levelId, HostWall2Start, HostWall2End);
        CreateWall(hostDocument, levelId, HostWall3Start, HostWall3End);
        CreateWall(hostDocument, levelId, HostWall4Start, HostWall4End);
        CreateWall(hostDocument, levelId, HostWall5Start, HostWall5End);
        CreateWall(hostDocument, levelId, HostWall6Start, HostWall6End);
        CreateFloor(hostDocument, level, floorType, HostFloor1);
        CreateFloor(hostDocument, level, floorType, HostFloor2);
        CreateDuct(hostDocument, ductSystemType.Id, ductType.Id, levelId, HostDuct1Start, HostDuct1End);
        CreatePipe(hostDocument, pipeSystemType.Id, pipeType.Id, levelId, HostPipe1Start, HostPipe1End);
        CreateTray(hostDocument, trayType.Id, levelId, HostTray1Start, HostTray1End);
        CreateDuct(hostDocument, ductSystemType.Id, ductType.Id, levelId, HostDuct2Start, HostDuct2End);
        CreatePipe(hostDocument, pipeSystemType.Id, pipeType.Id, levelId, HostPipe2Start, HostPipe2End);
        CreateDuct(hostDocument, ductSystemType.Id, ductType.Id, levelId, HostDuct3Start, HostDuct3End);
        CreateTray(hostDocument, trayType.Id, levelId, HostTray3Start, HostTray3End);
        SaveDocument(hostDocument, hostPath);

        LinkModels(hostDocument, linkAPath, linkBPath);

        // The host is shifted within its own site; walls drawn at -5..5 in the links then land on 0..10.
        ShiftHostInSharedSite(hostDocument, HostSharedEast);
        SaveDocument(hostDocument, hostPath);

        _logger.LogInformation(
            "Created collision test host {HostPath} with links {LinkAPath} and {LinkBPath}",
            hostPath,
            linkAPath,
            linkBPath);
        return new CollisionTestModelResult(hostDocument.Title, hostPath, linkAPath, linkBPath);
    }

    private static ElementId CreateLevel(Document document)
    {
        using Transaction transaction = new(document, "Create collision test level");
        transaction.Start();
        Level level = Level.Create(document, 0.0);
        transaction.Commit();
        return level.Id;
    }

    private static ElementId CreateWall(Document document, ElementId levelId, XYZ start, XYZ end)
    {
        using Transaction transaction = new(document, "Create collision test wall");
        transaction.Start();
        Wall wall = Wall.Create(document, Line.CreateBound(start, end), levelId, false);
        transaction.Commit();
        return wall.Id;
    }

    private static void DeleteElements(Document document, IReadOnlyList<ElementId> elementIds)
    {
        using Transaction transaction = new(document, "Remove collision test elements");
        transaction.Start();
        foreach (ElementId elementId in elementIds)
            document.Delete(elementId);
        transaction.Commit();
    }

    private static ElementId CreateFloor(
        Document document,
        Level level,
        FloorType floorType,
        (double MinX, double MinY, double MaxX, double MaxY) rectangle)
    {
        using Transaction transaction = new(document, "Create collision test floor");
        transaction.Start();
        List<Curve> edges =
        [
            Line.CreateBound(
                new XYZ(rectangle.MinX, rectangle.MinY, 0), new XYZ(rectangle.MaxX, rectangle.MinY, 0)),
            Line.CreateBound(
                new XYZ(rectangle.MaxX, rectangle.MinY, 0), new XYZ(rectangle.MaxX, rectangle.MaxY, 0)),
            Line.CreateBound(
                new XYZ(rectangle.MaxX, rectangle.MaxY, 0), new XYZ(rectangle.MinX, rectangle.MaxY, 0)),
            Line.CreateBound(
                new XYZ(rectangle.MinX, rectangle.MaxY, 0), new XYZ(rectangle.MinX, rectangle.MinY, 0)),
        ];
        Floor floor = CreateFloorCore(document, edges, floorType, level);
        transaction.Commit();
        return floor.Id;
    }

    // Floors are created through Document.Create in Revit 2021 and through Floor.Create
    // since Revit 2022; the version split lives in this helper rather than at the call site.
    private static Floor CreateFloorCore(
        Document document, List<Curve> edges, FloorType floorType, Level level)
    {
#if IS2021
        CurveArray profile = new();
        foreach (Curve edge in edges)
            profile.Append(edge);
        return document.Create.NewFloor(profile, floorType, level, false);
#else
        return Floor.Create(document, [CurveLoop.Create(edges)], floorType.Id, level.Id);
#endif
    }

    private static ElementId CreateDuct(
        Document document, ElementId systemTypeId, ElementId ductTypeId, ElementId levelId, XYZ start, XYZ end)
    {
        using Transaction transaction = new(document, "Create collision test duct");
        transaction.Start();
        Duct duct = Duct.Create(document, systemTypeId, ductTypeId, levelId, start, end);
        transaction.Commit();
        return duct.Id;
    }

    private static ElementId CreatePipe(
        Document document, ElementId systemTypeId, ElementId pipeTypeId, ElementId levelId, XYZ start, XYZ end)
    {
        using Transaction transaction = new(document, "Create collision test pipe");
        transaction.Start();
        Pipe pipe = Pipe.Create(document, systemTypeId, pipeTypeId, levelId, start, end);
        transaction.Commit();
        return pipe.Id;
    }

    private static void CreateTray(
        Document document, ElementId trayTypeId, ElementId levelId, XYZ start, XYZ end)
    {
        using Transaction transaction = new(document, "Create collision test cable tray");
        transaction.Start();
        CableTray.Create(document, trayTypeId, start, end, levelId);
        transaction.Commit();
    }

    private static FloorType GetDefaultFloorType(Document document) =>
        GetFirstType<FloorType>(document, "floor types");

    private static T GetFirstType<T>(Document document, string kind) where T : Element
    {
        T? type = new FilteredElementCollector(document)
            .OfClass(typeof(T))
            .Cast<T>()
            .FirstOrDefault();
        if (type is null)
            throw new InvalidOperationException(
                $"The collision test document has no {kind} to build the test runs.");
        return type;
    }

    private static void SaveDocument(Document document, string path)
    {
        ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(path);
        document.SaveAs(modelPath, new SaveAsOptions { OverwriteExistingFile = true });
    }

    private static void LinkModels(Document hostDocument, string linkAPath, string linkBPath)
    {
        using Transaction transaction = new(hostDocument, "Link collision test models by shared coordinates");
        transaction.Start();
        CreateSharedLink(hostDocument, linkAPath);
        CreateSharedLink(hostDocument, linkBPath);
        transaction.Commit();
    }

    private static void CreateSharedLink(Document hostDocument, string linkPath)
    {
        ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(linkPath);
        using RevitLinkOptions options = new(false);
        LinkLoadResult loadResult = RevitLinkType.Create(hostDocument, modelPath, options);
        if (loadResult.ElementId == ElementId.InvalidElementId)
            throw new InvalidOperationException(
                $"The collision test link '{linkPath}' could not be created: {loadResult.LoadResult}.");
        RevitLinkInstance.Create(hostDocument, loadResult.ElementId, ImportPlacement.Shared);
    }

    private static void ShiftHostInSharedSite(Document hostDocument, double sharedEast)
    {
        using Transaction transaction = new(hostDocument, "Shift collision test host in the shared site");
        transaction.Start();
        ProjectPosition position = hostDocument.Application.Create.NewProjectPosition(sharedEast, 0.0, 0.0, 0.0);
        hostDocument.ActiveProjectLocation.SetProjectPosition(XYZ.Zero, position);
        transaction.Commit();
    }
}
