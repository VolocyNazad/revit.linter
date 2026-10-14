using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceElevationIsTolerant;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceMirrored;
using Revit.Linter.Testing;
using Revit.TransactionMemoryCache.Services;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class FamilyInstanceDiagnosticsTests : RevitApiTest
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
    public async Task Instance_with_exact_elevation_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        var diagnostic = new FamilyInstanceElevationIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_with_tolerant_elevation_is_reported()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        using (Transaction transaction = new(document, "Offset test instance elevation"))
        {
            transaction.Start();
            instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(0.3 * MmInFeet);
            transaction.Commit();
        }
        var diagnostic = new FamilyInstanceElevationIsTolerantDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Instance_at_nearest_level_is_valid()
    {
        Document document = _document!;
        // A new project document already contains levels at zero elevation, which
        // would win the nearest-level tie: the test levels avoid that elevation.
        Level lower = CreateLevel(document, 5000);
        CreateLevel(document, 8000);
        FamilyInstance instance = PlaceInstance(Application, document, lower);
        var diagnostic = new FamilyInstanceLevelIsNearestDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Instance_far_from_associated_level_is_reported()
    {
        Document document = _document!;
        Level lower = CreateLevel(document, 0);
        Level upper = CreateLevel(document, 3000);
        FamilyInstance instance = PlaceInstance(Application, document, lower);
        using (Transaction transaction = new(document, "Move test instance association"))
        {
            transaction.Start();
            instance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).Set(upper.Id);
            instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(-2900 * MmInFeet);
            transaction.Commit();
        }
        var diagnostic = new FamilyInstanceLevelIsNearestDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Moved_level_is_reevaluated_with_shared_queries()
    {
        // Level elevations are cached per run: with a null cache every Execute rereads them,
        // so moving a level between two runs with one shared service must flip the verdict.
        // This guards the diagnostic against pinning stale elevations; production freshness
        // after a change is the cache invalidation contract.
        // Test levels sit far above the default zero-elevation levels of a new document,
        // so the nearest level is unambiguous with a wide margin in both states.
        Document document = _document!;
        Level lower = CreateLevel(document, 50000);
        Level upper = CreateLevel(document, 53000);
        FamilyInstance instance = PlaceInstance(Application, document, lower);
        using (Transaction transaction = new(document, "Move test instance association"))
        {
            transaction.Start();
            instance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).Set(upper.Id);
            instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(-2900 * MmInFeet);
            transaction.Commit();
        }
        var diagnostic = new FamilyInstanceLevelIsNearestDiagnostic(
            TestDocumentQueries.Create(NullRevitTransactionMemoryCache.Instance));

        DiagnosticFeedback before = diagnostic.Execute(document, null, instance);

        using (Transaction transaction = new(document, "Move test level elevation"))
        {
            transaction.Start();
            lower.Elevation = 70000 * MmInFeet;
            transaction.Commit();
        }
        DiagnosticFeedback after = diagnostic.Execute(document, null, instance);

        await Assert.That(before.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
        await Assert.That(after.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Unmirrored_instance_is_valid()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        var diagnostic = new FamilyInstanceMirroredDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, instance);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Mirrored_instance_is_reported()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        using (Transaction transaction = new(document, "Mirror test instance"))
        {
            transaction.Start();
            ElementTransformUtils.MirrorElements(
                document,
                [instance.Id],
                Plane.CreateByNormalAndOrigin(XYZ.BasisX, XYZ.Zero),
                false);
            transaction.Commit();
        }
        FamilyInstance mirrored = new FilteredElementCollector(document)
            .OfClass(typeof(FamilyInstance))
            .Cast<FamilyInstance>()
            .First(candidate => candidate.Mirrored);
        var diagnostic = new FamilyInstanceMirroredDiagnostic();

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, mirrored);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Only_instances_are_checked_by_instance_filters()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        var elevationFilter = new FamilyInstanceElevationIsTolerantDiagnosticFilter();
        var mirroredFilter = new FamilyInstanceMirroredDiagnosticFilter();

        await Assert.That(elevationFilter.IsRelevantFor(document, instance)).IsTrue();
        await Assert.That(mirroredFilter.IsRelevantFor(document, instance)).IsTrue();
        await Assert.That(elevationFilter.IsRelevantFor(document, level)).IsFalse();
        await Assert.That(mirroredFilter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Only_unhosted_instances_are_checked_by_level_filter()
    {
        Document document = _document!;
        Level level = CreateLevel(document, 0);
        FamilyInstance instance = PlaceInstance(Application, document, level);
        var filter = new FamilyInstanceLevelIsNearestDiagnosticFilter();

        await Assert.That(filter.IsRelevantFor(document, instance)).IsTrue();
        await Assert.That(filter.IsRelevantFor(document, level)).IsFalse();
    }

    [Test]
    public async Task Instance_checks_run_in_expected_documents()
    {
        var elevationFilter = new FamilyInstanceElevationIsTolerantDiagnosticDocumentFilter();
        var levelFilter = new FamilyInstanceLevelIsNearestDiagnosticDocumentFilter();
        var mirroredFilter = new FamilyInstanceMirroredDiagnosticDocumentFilter();
        await Assert.That(elevationFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(levelFilter.IsRelevantFor(_document!)).IsTrue();
        await Assert.That(mirroredFilter.IsRelevantFor(_document!)).IsTrue();

        Document familyDocument = FamilyTestDocuments.OpenModelFamilyTemplate(Application);
        try
        {
            await Assert.That(elevationFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(levelFilter.IsRelevantFor(familyDocument)).IsFalse();
            await Assert.That(mirroredFilter.IsRelevantFor(familyDocument)).IsTrue();
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

    private static FamilyInstance PlaceInstance(
        Autodesk.Revit.ApplicationServices.Application application, Document document, Level level)
    {
        Family family = FamilyTestDocuments.LoadFamilyWithType(application, document, "LinterT1");
        FamilySymbol symbol = new FilteredElementCollector(document)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .First(candidate => candidate.Family.Id == family.Id);
        using Transaction transaction = new(document, "Place test family instance");
        transaction.Start();
        FamilyInstance instance = FamilyTestDocuments.PlaceInstance(document, symbol, level, XYZ.Zero);
        transaction.Commit();
        return instance;
    }
}
