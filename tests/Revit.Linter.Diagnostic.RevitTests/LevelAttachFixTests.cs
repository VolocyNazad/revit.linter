using Autodesk.Revit.DB;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.ElementDiagnostics.Diagnostics.FamilyInstanceLevelIsNearest;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class LevelAttachFixTests : RevitApiTest
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
    public async Task Attach_to_nearest_level_reassigns_instance()
    {
        Document document = _document!;
        // Levels avoid the default zero elevation: it would tie with a test level
        // and the older default element would win the stable ordering.
        Level lower = CreateLevel(document, 5000);
        Level upper = CreateLevel(document, 8000);
        FamilyInstance instance = PlaceInstance(Application, document, lower);
        using (Transaction transaction = new(document, "Move test instance association"))
        {
            transaction.Start();
            instance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).Set(upper.Id);
            instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(-3000 * MmInFeet);
            transaction.Commit();
        }
        var fix = new AttachToNearestLevel();

        bool result;
        using (Transaction transaction = new(document, "Attach test instance to nearest level"))
        {
            transaction.Start();
            result = fix.Execute(instance);
            transaction.Commit();
        }
        ElementId levelId = ((FamilyInstance)document.GetElement(instance.Id))
            .get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0012");
        await Assert.That(result).IsTrue();
        await Assert.That(levelId).IsEqualTo(lower.Id);
    }

    [Test]
    public async Task Instance_at_nearest_level_is_kept()
    {
        Document document = _document!;
        // A new project document already contains levels at zero elevation, which
        // would win the nearest-level tie: the test levels avoid that elevation.
        Level lower = CreateLevel(document, 5000);
        CreateLevel(document, 8000);
        FamilyInstance instance = PlaceInstance(Application, document, lower);
        var fix = new AttachToNearestLevel();

        bool result;
        using (Transaction transaction = new(document, "Attach test instance to nearest level"))
        {
            transaction.Start();
            result = fix.Execute(instance);
            transaction.Commit();
        }
        ElementId levelId = ((FamilyInstance)document.GetElement(instance.Id))
            .get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();

        await Assert.That(result).IsFalse();
        await Assert.That(levelId).IsEqualTo(lower.Id);
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
