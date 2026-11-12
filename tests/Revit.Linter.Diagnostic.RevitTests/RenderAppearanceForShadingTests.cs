using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Nice3point.TUnit.Revit;
using Nice3point.TUnit.Revit.Executors;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.ElementDiagnostics.Diagnostics.RenderAppearanceForShading;
using Revit.Linter.Testing;
using TUnit.Core.Executors;

namespace Revit.Linter.Diagnostic.RevitTests;

public sealed class RenderAppearanceForShadingTests : RevitApiTest
{
    private const string TextureAssetName = "UnifiedBitmap";

    private static readonly string[] Channels =
    [
        Generic.GenericDiffuse,
        Generic.GenericBumpMap,
        Generic.GenericTransparency,
        Generic.GenericGlossiness,
    ];

    private Document? _document;

    [Before(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CreateDocument() => _document = Application.NewProjectDocument(UnitSystem.Metric);

    [After(Test)]
    [HookExecutor<RevitThreadExecutor>]
    public void CloseDocument() => _document?.Close(false);

    [Test]
    public async Task Bitmap_render_appearance_used_for_shading_is_reported()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateBitmapAppearanceAsset(document, "LinterBitmapAppearance");
        Material material = CreateMaterial(document, "LinterShadedBitmapMaterial", appearanceAsset.Id, true);
        // The test query cache never invalidates: the diagnostic is created after every
        // document change so the asset classification is built over the final state.
        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Procedural_render_appearance_used_for_shading_is_reported()
    {
        // The installed material library contains procedural appearances (for example noise) that
        // carry connected properties instead of a bitmap file: they are textures too.
        Document document = _document!;
        AppearanceAssetElement appearanceAsset =
            CreateLibraryAppearanceAsset(document, "LinterProceduralAppearance", hasConnectedTexture: true);
        Material material = CreateMaterial(document, "LinterShadedProceduralMaterial", appearanceAsset.Id, true);
        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.NotValid);
    }

    [Test]
    public async Task Textured_render_appearance_without_shading_flag_is_valid()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateBitmapAppearanceAsset(document, "LinterTextureNotShadedAppearance");
        Material material = CreateMaterial(document, "LinterTextureNotShadedMaterial", appearanceAsset.Id, false);
        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Shading_flag_without_appearance_asset_is_valid()
    {
        Document document = _document!;
        Material material = CreateMaterial(
            document, "LinterShadingWithoutAssetMaterial", ElementId.InvalidElementId, true);
        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Shading_flag_with_untextured_appearance_asset_is_valid()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset =
            CreateLibraryAppearanceAsset(document, "LinterUntexturedAppearance", hasConnectedTexture: false);
        Material material = CreateMaterial(document, "LinterUntexturedShadingMaterial", appearanceAsset.Id, true);
        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());

        DiagnosticFeedback feedback = diagnostic.Execute(document, null, material);

        await Assert.That(feedback.Verdict).IsEqualTo(DiagnosticVerdict.Valid);
    }

    [Test]
    public async Task Disable_shading_fix_turns_setting_off()
    {
        Document document = _document!;
        AppearanceAssetElement appearanceAsset = CreateBitmapAppearanceAsset(document, "LinterFixedAppearance");
        Material material = CreateMaterial(document, "LinterFixedShadingMaterial", appearanceAsset.Id, true);
        var fix = new DisableRenderAppearanceForShading();

        bool result;
        using (Transaction transaction = new(document, "Disable render appearance for shading"))
        {
            transaction.Start();
            result = fix.Execute(material);
            transaction.Commit();
        }

        var diagnostic = new RenderAppearanceForShadingDiagnostic(TestDocumentQueries.Create());
        await Assert.That(result).IsTrue();
        await Assert.That(material.UseRenderAppearanceForShading).IsFalse();
        await Assert.That(diagnostic.Execute(document, null, material).Verdict)
            .IsEqualTo(DiagnosticVerdict.Valid);
    }

    private static AppearanceAssetElement CreateBitmapAppearanceAsset(Document document, string name)
    {
        AppearanceAssetElement appearanceAsset =
            CreateLibraryAppearanceAsset(document, name, hasConnectedTexture: false);
        using Transaction transaction = new(document, "Add bitmap texture to test appearance asset");
        transaction.Start();
        using (AppearanceAssetEditScope editScope = new(document))
        {
            Asset editableAsset = editScope.Start(appearanceAsset.Id);
            AssetProperty? diffuse = editableAsset.FindByName(Generic.GenericDiffuse);
            if (diffuse is null)
                throw new InvalidOperationException(
                    $"Preset appearance asset has no {Generic.GenericDiffuse} property to connect a texture to.");
            if (diffuse.NumberOfConnectedProperties == 0)
                diffuse.AddConnectedAsset(TextureAssetName);
            editScope.Commit(false);
        }

        transaction.Commit();
        return appearanceAsset;
    }

    private static AppearanceAssetElement CreateLibraryAppearanceAsset(
        Document document, string name, bool hasConnectedTexture)
    {
        foreach (Asset preset in Application.GetAssets(AssetType.Appearance))
        {
            if (HasConnectedChannel(preset) != hasConnectedTexture) continue;
            using Transaction transaction = new(document, "Create test appearance asset");
            transaction.Start();
            AppearanceAssetElement appearanceAsset = AppearanceAssetElement.Create(document, name, preset);
            transaction.Commit();
            return appearanceAsset;
        }

        throw new InvalidOperationException(
            $"Revit returned no appearance asset with connected texture: {hasConnectedTexture}.");
    }

    private static bool HasConnectedChannel(Asset asset)
        => Channels.Any(channel => asset.FindByName(channel) is { NumberOfConnectedProperties: > 0 });

    private static Material CreateMaterial(
        Document document,
        string name,
        ElementId appearanceAssetId,
        bool useRenderAppearanceForShading)
    {
        using Transaction transaction = new(document, "Create test material");
        transaction.Start();
        ElementId materialId = Material.Create(document, name);
        var material = (Material)document.GetElement(materialId);
        if (appearanceAssetId != ElementId.InvalidElementId)
            material.AppearanceAssetId = appearanceAssetId;
        material.UseRenderAppearanceForShading = useRenderAppearanceForShading;
        transaction.Commit();
        return material;
    }
}
