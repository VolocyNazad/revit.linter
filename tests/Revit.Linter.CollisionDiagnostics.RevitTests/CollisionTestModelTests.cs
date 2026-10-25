using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Microsoft.Extensions.Logging.Abstractions;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Extensions;
using Revit.Linter.CollisionDiagnostics.Infrastructure.Services;
using Revit.Linter.CollisionDiagnostics.RevitTests.Infrastructure;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Linter.Languages.Factories;
using Revit.Linter.Testing;
using Revit.Sugar;
using TUnit.Core.Executors;
using CollisionElementDiagnostic = Revit.Linter.CollisionDiagnostics.ElementDiagnostic;

namespace Revit.Linter.CollisionDiagnostics.RevitTests;

/// <summary>
/// Covers the collision-testing documents built by <c>CollisionTestModelService</c>:
/// linked walls and floors land on host walls and floors, and the touch-only
/// and isolated controls stay silent. Link expectations follow the text-only link reporting:
/// linked hits never enter the dependency list.
/// </summary>
public sealed class CollisionTestModelTests : RevitApiTest
{
    private const double Tolerance = 1e-6;

    private Document? _host;
    private string? _directory;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateModel()
    {
        CollisionTestModelResult result = new CollisionTestModelService().CreateHostWithTwoLinks(Application);
        _directory = Path.GetDirectoryName(result.HostPath);
        // Parallel tests build identically named hosts: match ours by saved path, not by title.
        _host = Application.Documents
            .OfType<Document>()
            .FirstOrDefault(candidate =>
                string.Equals(candidate.PathName, result.HostPath, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"The collision test host '{result.HostPath}' was not found among open documents.");
    }

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseModel()
    {
        _host?.Close(false);
        _host = null;
        if (_directory is not null && Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        _directory = null;
    }

    [Test]
    public async Task Host_isolated_floor_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Floors'))", includeLinks: true);

        Floor hostFloor2 = FindHostFloor(100, 100, 110, 108);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostFloor2);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Host_isolated_wall_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Walls'))", includeLinks: true);

        Wall hostWall4 = FindHostWall(50, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostWall4);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Touching_host_wall_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Walls'))", includeLinks: true);

        Wall hostWall5 = FindHostWall(60, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostWall5);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Link_wall_hits_host_wall()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Walls'))", includeLinks: true);

        Wall hostWall1 = FindHostWall(0, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostWall1);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkA]");
        await Assert.That(count).IsNotEqualTo(0);
    }

    [Test]
    public async Task Second_link_wall_hits_its_host_wall()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Walls'))", includeLinks: true);

        Wall hostWall3 = FindHostWall(30, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostWall3);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkB]");
        await Assert.That(count).IsNotEqualTo(0);
    }

    [Test]
    public async Task Link_cross_wall_hits_joint_free_host_wall()
    {
        // H6 stands clear of every host joint, so unlike the H1/H2 crossing its solid keeps
        // full volume along the crossing: the joint-free host-versus-link cross hit.
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Walls'))", includeLinks: true);

        Wall hostWall6 = FindHostWall(80, 0, 0, 1);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostWall6);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkB]");
        await Assert.That(count).IsNotEqualTo(0);
    }

    [Test]
    public async Task Link_floor_hits_host_floor()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_Floors'))", includeLinks: true);

        Floor hostFloor1 = FindHostFloor(0, 0, 10, 8);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostFloor1);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkA]");
    }

    private Wall FindHostWall(double originX, double originY, double directionX, double directionY)
    {
        foreach (Wall wall in new FilteredElementCollector(_host).OfClass(typeof(Wall)).Cast<Wall>())
        {
            if (wall.Location is not LocationCurve location || location.Curve is not Line line)
                continue;
            if (Math.Abs(line.Origin.X - originX) < Tolerance
                && Math.Abs(line.Origin.Y - originY) < Tolerance
                && Math.Abs(line.Direction.X - directionX) < Tolerance
                && Math.Abs(line.Direction.Y - directionY) < Tolerance)
                return wall;
        }

        throw new InvalidOperationException(
            $"Host wall at ({originX}, {originY}) along ({directionX}, {directionY}) was not found.");
    }

    private Floor FindHostFloor(double minX, double minY, double maxX, double maxY)
    {
        foreach (Floor floor in new FilteredElementCollector(_host).OfClass(typeof(Floor)).Cast<Floor>())
        {
            BoundingBoxXYZ? box = floor.get_BoundingBox(null);
            if (box is null)
                continue;
            if (Math.Abs(box.Min.X - minX) < Tolerance
                && Math.Abs(box.Min.Y - minY) < Tolerance
                && Math.Abs(box.Max.X - maxX) < Tolerance
                && Math.Abs(box.Max.Y - maxY) < Tolerance
                && Math.Abs(box.Max.Z) < 0.05
                && box.Max.Z > box.Min.Z + 0.05)
                return floor;
        }

        throw new InvalidOperationException(
            $"Host floor ({minX}, {minY})-({maxX}, {maxY}) was not found.");
    }

    [Test]
    public async Task Host_duct_crosses_host_pipe()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: false);

        Duct hostDuct1 = FindHostRun<Duct>(0, 2, 9, 1, 0, 0);
        Pipe hostPipe1 = FindHostRun<Pipe>(5, -2, 9, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostDuct1);
        long[] dependencyIds = [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id.IdValue())];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).Contains(hostPipe1.Id.Value());
    }

    [Test]
    public async Task Host_isolated_duct_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: true);

        Duct hostDuct3 = FindHostRun<Duct>(100, 102, 9, 1, 0, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostDuct3);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Link_pipe_hits_host_duct()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: true);

        Duct hostDuct1 = FindHostRun<Duct>(0, 2, 9, 1, 0, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostDuct1);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkA]");
        await Assert.That(count).IsNotEqualTo(0);
    }

    [Test]
    public async Task Second_link_pipe_hits_its_host_duct()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: true);

        Duct hostDuct2 = FindHostRun<Duct>(30, 2, 9, 1, 0, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostDuct2);
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];
        int count = (int)feedback.AdditionalMessageArguments["intersection.count"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(elementNames).Contains("[LinterCollisionLinkB]");
        await Assert.That(count).IsNotEqualTo(0);
    }

    [Test]
    public async Task Tray_hits_duct_and_pipe()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: true);

        CableTray hostTray1 = FindHostRun<CableTray>(0, 2, 9, 1, 0, 0);
        Duct hostDuct1 = FindHostRun<Duct>(0, 2, 9, 1, 0, 0);
        Pipe hostPipe1 = FindHostRun<Pipe>(5, -2, 9, 0, 1, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostTray1);
        long[] dependencyIds = [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id.IdValue())];
        string elementNames = (string)feedback.AdditionalMessageArguments!["intersection.elementNames"];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).Contains(hostDuct1.Id.Value());
        await Assert.That(dependencyIds).Contains(hostPipe1.Id.Value());
        await Assert.That(elementNames).Contains("[LinterCollisionLinkA]");
    }

    [Test]
    public async Task Host_isolated_tray_is_valid()
    {
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_PipeCurves'))",
            includeLinks: true);

        CableTray hostTray3 = FindHostRun<CableTray>(100, 104, 9, 1, 0, 0);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, hostTray3);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    // Generic helpers must not constrain their type to a Revit API type: resolving
    // such a constraint before Revit is injected breaks the test session setup.
    // The constraint stays at the object level instead, with casts inside.
    private T FindHostRun<T>(
        double originX, double originY, double originZ, double directionX, double directionY, double directionZ)
        where T : class
    {
        foreach (Element run in new FilteredElementCollector(_host).OfClass(typeof(T)).Cast<Element>())
        {
            if (run.Location is not LocationCurve location || location.Curve is not Line line)
                continue;
            if (Math.Abs(line.Origin.X - originX) < Tolerance
                && Math.Abs(line.Origin.Y - originY) < Tolerance
                && Math.Abs(line.Origin.Z - originZ) < Tolerance
                && Math.Abs(line.Direction.X - directionX) < Tolerance
                && Math.Abs(line.Direction.Y - directionY) < Tolerance
                && Math.Abs(line.Direction.Z - directionZ) < Tolerance)
                return (T)(object)run;
        }

        throw new InvalidOperationException(
            $"Host {typeof(T).Name} run at ({originX}, {originY}, {originZ}) was not found.");
    }

    [Test]
    public async Task Joined_ducts_are_silent_with_flag()
    {
        (_, Duct second, _) = CreateConnectedDuctPair();
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_DuctFitting'))",
            includeLinks: false,
            ignoreConnectedElements: true);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, second);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Joined_ducts_are_reported_without_flag()
    {
        // The coincident pair genuinely overlaps: without the flag the joint
        // must be reported, which also proves the silence above comes from the flag.
        (Duct first, Duct second) = CreateCoincidentDuctPair();
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_DuctFitting'))",
            includeLinks: false,
            ignoreConnectedElements: false);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, first);
        long[] dependencyIds = [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id.IdValue())];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).Contains(second.Id.Value());
    }

    [Test]
    public async Task Unjoined_crossing_duct_is_reported_with_flag()
    {
        (Duct first, _, Duct crossing) = CreateConnectedDuctPair();
        CollisionElementDiagnostic diagnostic = CreateDiagnostic(
            "instance and (builtincategory('OST_DuctCurves') or builtincategory('OST_DuctFitting'))",
            includeLinks: false,
            ignoreConnectedElements: true);

        DiagnosticFeedback feedback = diagnostic.Execute(_host!, null, crossing);
        long[] dependencyIds = [.. feedback.AdditionalTargetDependencies.OfType<Element>().Select(element => element.Id.IdValue())];

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(dependencyIds).Contains(first.Id.Value());
    }

    // Two collinear ducts joined end to end by a direct connector joint (an empty document
    // has no fitting family for NewElbowFitting) plus a third duct crossing the first one
    // without any connection. The joint area is clear of every other test-model element.
    private (Duct First, Duct Second, Duct Crossing) CreateConnectedDuctPair()
    {
        Level level = new FilteredElementCollector(_host).OfClass(typeof(Level)).Cast<Level>().First();
        MechanicalSystemType systemType = FirstType<MechanicalSystemType>();
        DuctType ductType = FirstType<DuctType>();

        using Transaction transaction = new(_host, "Create connected duct pair for collision tests");
        transaction.Start();
        Duct first = Duct.Create(
            _host, systemType.Id, ductType.Id, level.Id, new XYZ(100, 120, 9), new XYZ(112, 120, 9));
        Duct second = Duct.Create(
            _host, systemType.Id, ductType.Id, level.Id, new XYZ(112, 120, 9), new XYZ(124, 120, 9));
        Duct crossing = Duct.Create(
            _host, systemType.Id, ductType.Id, level.Id, new XYZ(106, 114, 9), new XYZ(106, 126, 9));
        XYZ joint = new(112, 120, 9);
        FindConnectorNear(first, joint).ConnectTo(FindConnectorNear(second, joint));
        transaction.Commit();
        return (first, second, crossing);
    }

    // Two coincident ducts joined end to end by a direct connector joint: the pair
    // genuinely overlaps, so it is reported unless connected elements are ignored.
    // Placed clear of every other test-model element.
    private (Duct First, Duct Second) CreateCoincidentDuctPair()
    {
        Level level = new FilteredElementCollector(_host).OfClass(typeof(Level)).Cast<Level>().First();
        MechanicalSystemType systemType = FirstType<MechanicalSystemType>();
        DuctType ductType = FirstType<DuctType>();

        using Transaction transaction = new(_host, "Create coincident duct pair for collision tests");
        transaction.Start();
        Duct first = Duct.Create(
            _host, systemType.Id, ductType.Id, level.Id, new XYZ(140, 120, 9), new XYZ(150, 120, 9));
        Duct second = Duct.Create(
            _host, systemType.Id, ductType.Id, level.Id, new XYZ(140, 120, 9), new XYZ(150, 120, 9));
        FindConnectorNear(first, new XYZ(140, 120, 9)).ConnectTo(FindConnectorNear(second, new XYZ(150, 120, 9)));
        transaction.Commit();
        return (first, second);
    }

    private static Connector FindConnectorNear(MEPCurve run, XYZ point)
    {
        Connector? nearest = null;
        double nearestDistance = double.MaxValue;
        foreach (Connector connector in run.ConnectorManager.Connectors)
        {
            double distance = connector.Origin.DistanceTo(point);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            nearest = connector;
        }

        if (nearest is null)
            throw new InvalidOperationException(
                $"The {run.Id.Value()} run has no connectors to join.");
        return nearest;
    }

    private T FirstType<T>() where T : class
    {
        T? type = new FilteredElementCollector(_host).OfClass(typeof(T)).Cast<T>().FirstOrDefault();
        if (type is null)
            throw new InvalidOperationException(
                $"The collision test document has no {typeof(T).Name} to build the connected corner.");
        return type;
    }

    private static CollisionElementDiagnostic CreateDiagnostic(string take, bool includeLinks, bool ignoreConnectedElements = false)
    {
        // A new query service per diagnostic: the test cache never invalidates on transactions.
        IDocumentQueryService queries = TestDocumentQueries.Create();
        FormulaCompilationNotifier notifier = new();
        return new CollisionElementDiagnostic(
            new ElementFilterFactory(NullLogger<ElementFilterFactory>.Instance, notifier),
            new ElementFunctionFactory(NullLogger<ElementFunctionFactory>.Instance, notifier),
            new GetElementBoundingBoxService(queries),
            new GetElementGeometryService(queries),
            new GetConnectedElementIdsService(queries),
            queries,
            NullLogger<CollisionElementDiagnostic>.Instance)
        {
            Identity = new ElementDiagnosticId(
                "CLSN-TEST", "Description", "Message", DiagnosticSeverity.Warning, true, false, ""),
            TakeFormula = take,
            GroupByFormula = "'all'",
            IncludeLinks = includeLinks,
            IgnoreConnectedElements = ignoreConnectedElements,
        };
    }

    private sealed class FormulaCompilationNotifier : IFormulaCompilationNotifier
    {
        public void Notify()
        {
            // Compilation failures are not part of what these tests assert.
        }
    }
}
