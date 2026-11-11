using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.UnusedAppearanceAsset;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class UnusedAppearanceAssetTests : RevitApiTest
{
    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Unused_appearance_asset_is_reported()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateAppearanceAsset(document, "LinterUnusedAppearanceAsset");
        // The test query cache never invalidates: the diagnostic is created after every document
        // change so the used-appearance-asset set is built over the final state.
        var diagnostic = new UnusedAppearanceAssetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, appearanceAsset);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Referenced_appearance_asset_is_valid()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateAppearanceAsset(document, "LinterUsedAppearanceAsset");
        Material material = CreateMaterial(document, "LinterAppearanceMaterial");
        AssignAppearanceAsset(document, material, appearanceAsset.Id);
        var diagnostic = new UnusedAppearanceAssetDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, appearanceAsset);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Delete_unused_appearance_asset_removes_asset()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateAppearanceAsset(document, "LinterDeletedAppearanceAsset");
        ElementId appearanceAssetId = appearanceAsset.Id;
        var fix = new DeleteUnusedAppearanceAsset();

        bool result;
        using (Transaction transaction = new(document, "Delete test appearance asset"))
        {
            transaction.Start();
            result = fix.Execute(appearanceAsset);
            transaction.Commit();
        }

        await Assert.That(fix.Identity.Code).IsEqualTo("SHRD0039");
        await Assert.That(result).IsTrue();
        await Assert.That(document.GetElement(appearanceAssetId)).IsNull();
    }

    [Test]
    public async Task Appearance_asset_is_part_of_the_full_element_list()
    {
        // The diagnostic only receives elements the run collected: an appearance asset must be
        // reachable through the shared all-elements query, or the check would never run.
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateAppearanceAsset(document, "LinterListedAppearanceAsset");
        IReadOnlyList<Element> elements = TestDocumentQueries.Create().GetElements(document);

        await Assert.That(elements.Any(element => element.Id == appearanceAsset.Id)).IsTrue();
    }

    private static AppearanceAssetElement CreateAppearanceAsset(Document document, string name)
    {
        IList<Asset> presetAssets = Application.GetAssets(AssetType.Appearance);
        if (presetAssets.Count == 0)
            throw new InvalidOperationException("Revit returned no preset appearance assets.");
        using Transaction transaction = new(document, "Create test appearance asset");
        transaction.Start();
        AppearanceAssetElement appearanceAsset = AppearanceAssetElement.Create(document, name, presetAssets[0]);
        transaction.Commit();
        return appearanceAsset;
    }

    private static Material CreateMaterial(Document document, string name)
    {
        using Transaction transaction = new(document, "Create test material");
        transaction.Start();
        ElementId materialId = Material.Create(document, name);
        transaction.Commit();
        return (Material)document.GetElement(materialId);
    }

    private static void AssignAppearanceAsset(Document document, Material material, ElementId appearanceAssetId)
    {
        using Transaction transaction = new(document, "Assign test material appearance asset");
        transaction.Start();
        material.AppearanceAssetId = appearanceAssetId;
        transaction.Commit();
    }
}
