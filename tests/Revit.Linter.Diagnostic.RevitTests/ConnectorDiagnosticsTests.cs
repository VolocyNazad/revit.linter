using Autodesk.Revit.DB;
#if !IS2021
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
#endif
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.AllConnectorsConnected;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class ConnectorDiagnosticsTests : RevitApiTest
{
    private const double MmInFeet = 1.0 / 304.8;

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Only_end_and_physical_connectors_are_checked()
    {
        ConnectorType[] checkedTypes = Enum.GetValues(typeof(ConnectorType))
            .Cast<ConnectorType>()
            .Where(AnyConnectorsNotConnectedDiagnostic.IsCheckableConnector)
            .ToArray();

        await Assert.That(checkedTypes.Length).IsEqualTo(2);
        await Assert.That(checkedTypes.Contains(ConnectorType.End)).IsTrue();
        await Assert.That(checkedTypes.Contains(ConnectorType.Physical)).IsTrue();
    }

    [Test]
    public async Task Check_runs_in_projects_only()
    {
        var filter = new AnyConnectorsNotConnectedDiagnosticDocumentFilter();
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

    // Connector-cube tests are disabled on Revit 2021: NewExtrusion is not permitted
    // in the picked template family there.
#if !IS2021
    [Test]
    public async Task Only_instances_with_mep_model_are_checked_by_filter()
    {
        Document document = _document!;
        FamilyInstance withConnectors = PlaceConnectorInstance(Application, document, CreatePipeConnector);
        Level level = CreateLevel(document, 0);
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, family);
        FamilyInstance plain;
        using (Transaction transaction = new(document, "Place test family instance"))
        {
            transaction.Start();
            plain = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            transaction.Commit();
        }
        var filter = new AnyConnectorsNotConnectedDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, withConnectors)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, plain)).IsFalse();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Instance_with_unconnected_pipe_connector_is_reported()
    {
        FamilyInstance instance = PlaceConnectorInstance(Application, _document!, CreatePipeConnector);
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Instance_with_unconnected_duct_connector_is_reported()
    {
        FamilyInstance instance = PlaceConnectorInstance(
            Application, _document!,
            (familyDocument, face) => ConnectorElement.CreateDuctConnector(
                familyDocument, DuctSystemType.SupplyAir, ConnectorProfileType.Rectangular, face));
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Instance_with_unconnected_cable_tray_connector_is_reported()
    {
        FamilyInstance instance = PlaceConnectorInstance(
            Application, _document!,
            (familyDocument, face) =>
                ConnectorElement.CreateCableTrayConnector(familyDocument, face));
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Instance_with_unconnected_conduit_connector_is_reported()
    {
        FamilyInstance instance = PlaceConnectorInstance(
            Application, _document!,
            (familyDocument, face) => ConnectorElement.CreateConduitConnector(familyDocument, face));
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Instance_with_unconnected_electrical_connector_is_reported()
    {
        FamilyInstance instance = PlaceConnectorInstance(
            Application, _document!,
            (familyDocument, face) => ConnectorElement.CreateElectricalConnector(
                familyDocument, ElectricalSystemType.PowerCircuit, face));
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(_document!, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }
#endif

    [Test]
    public async Task Instance_without_mep_model_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        Family family = FamilyTestDocuments.LoadFamilyWithType(Application, document, "LinterT1");
        FamilySymbol symbol = FindSymbol(document, family);
        FamilyInstance instance;
        using (Transaction transaction = new(document, "Place test family instance"))
        {
            transaction.Start();
            instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            transaction.Commit();
        }
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

#if !IS2021
    [Test]
    public async Task Instance_with_connected_pipe_connector_is_valid()
    {
        DiagnosticFeedback feedback = ExecuteOnConnectedPair(Application, _document!, CreatePipeConnector);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_with_connected_duct_connector_is_valid()
    {
        DiagnosticFeedback feedback = ExecuteOnConnectedPair(
            Application, _document!,
            (familyDocument, face) => ConnectorElement.CreateDuctConnector(
                familyDocument, DuctSystemType.SupplyAir, ConnectorProfileType.Rectangular, face));

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_with_connected_cable_tray_connector_is_valid()
    {
        DiagnosticFeedback feedback = ExecuteOnConnectedPair(
            Application, _document!,
            (familyDocument, face) =>
                ConnectorElement.CreateCableTrayConnector(familyDocument, face));

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_with_connected_conduit_connector_is_valid()
    {
        DiagnosticFeedback feedback = ExecuteOnConnectedPair(
            Application, _document!,
            (familyDocument, face) => ConnectorElement.CreateConduitConnector(familyDocument, face));

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }
#endif

    // No connected-electrical test: Revit forbids device-to-device ConnectTo for
    // electrical connectors ("Electrical device or equipment cannot connect to
    // electrical device or equipment directly") — they connect only through a
    // Wire, which needs a WireType and a plan view that empty test documents
    // do not have. Same asset/template gap as the MEPCurve branch.

#if !IS2021
    private static DiagnosticFeedback ExecuteOnConnectedPair(
        Autodesk.Revit.ApplicationServices.Application application,
        Document document,
        Func<Document, Reference, ConnectorElement> createConnector)
    {
        Level level = CreateLevel(document, 0);
        Family family = CreateConnectorFamily(application, document, createConnector);
        FamilySymbol symbol = FindSymbol(document, family);
        FamilyInstance first;
        FamilyInstance second;
        using (Transaction transaction = new(document, "Place test instances"))
        {
            transaction.Start();
            first = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
            second = FamilyTestDocuments.PlaceInstance(
                document, symbol, level, new XYZ(0, 0, 1000 * MmInFeet));
            transaction.Commit();
        }
        // The instance MEP models materialize on commit: connecting inside the
        // placement transaction reads them before they exist.
        using (Transaction transaction = new(document, "Connect test instances"))
        {
            transaction.Start();
            FindConnector(first).ConnectTo(FindConnector(second));
            transaction.Commit();
        }
        var diagnostic = new AnyConnectorsNotConnectedDiagnostic();

        return diagnostic.Execute(document, null, first);
    }

    private static ConnectorElement CreatePipeConnector(Document familyDocument, Reference face) =>
        ConnectorElement.CreatePipeConnector(familyDocument, PipeSystemType.Sanitary, face);

    private static FamilyInstance PlaceConnectorInstance(
        Autodesk.Revit.ApplicationServices.Application application,
        Document document,
        Func<Document, Reference, ConnectorElement> createConnector)
    {
        Level level = CreateLevel(document, 0);
        Family family = CreateConnectorFamily(application, document, createConnector);
        FamilySymbol symbol = FindSymbol(document, family);
        using Transaction transaction = new(document, "Place test connector instance");
        transaction.Start();
        FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
        transaction.Commit();
        return instance;
    }

    private static Family CreateConnectorFamily(
        Autodesk.Revit.ApplicationServices.Application application,
        Document projectDocument,
        Func<Document, Reference, ConnectorElement> createConnector)
    {
        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(application);
        try
        {
            Extrusion cube;
            using (Transaction transaction = new(familyDocument, "Create test connector cube"))
            {
                transaction.Start();
                cube = CreateCube(familyDocument);
                transaction.Commit();
            }
            using (Transaction transaction = new(familyDocument, "Add test connector"))
            {
                transaction.Start();
                createConnector(familyDocument, FindTopFaceReference(cube));
                transaction.Commit();
            }
            return familyDocument.LoadFamily(projectDocument);
        }
        finally
        {
            familyDocument.Close(false);
        }
    }

    private static Extrusion CreateCube(Document familyDocument)
    {
        Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
        SketchPlane sketchPlane = SketchPlane.Create(familyDocument, plane);
        double half = 250 * MmInFeet;
        CurveArray rectangle = new();
        rectangle.Append(Line.CreateBound(new XYZ(-half, -half, 0), new XYZ(half, -half, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(half, -half, 0), new XYZ(half, half, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(half, half, 0), new XYZ(-half, half, 0)));
        rectangle.Append(Line.CreateBound(new XYZ(-half, half, 0), new XYZ(-half, -half, 0)));
        CurveArrArray profile = new();
        profile.Append(rectangle);
        return familyDocument.FamilyCreate.NewExtrusion(true, profile, sketchPlane, 500 * MmInFeet);
    }

    private static Reference FindTopFaceReference(Extrusion cube) =>
        cube.get_Geometry(new Options { ComputeReferences = true })
            .OfType<Solid>()
            .SelectMany(solid => solid.Faces.OfType<PlanarFace>())
            .FirstOrDefault(face => face.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
            ?.Reference
        ?? throw new InvalidOperationException("The test extrusion has no top planar face.");

    private static Connector FindConnector(FamilyInstance instance) =>
        instance.MEPModel!.ConnectorManager!.Connectors
            .Cast<Connector>()
            .First();
#endif

    private static FamilySymbol FindSymbol(Document document, Family family) =>
        new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .First(candidate => candidate.Family.Id == family.Id);

    private static Level CreateLevel(Document document, double elevationMillimeters)
    {
        using Transaction transaction = new(document, "Create test level");
        transaction.Start();
        Level level = Level.Create(document, elevationMillimeters * MmInFeet);
        transaction.Commit();
        return level;
    }
}
