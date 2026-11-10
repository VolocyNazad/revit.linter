using System.Globalization;
using Revit.Linter.DocumentQueries.Abstractions.Models;
using Revit.Linter.DocumentQueries.Abstractions.Services;
using Revit.Sugar;

namespace Revit.Linter.ElementDiagnostics.Diagnostics.DuplicateMaterial;

internal sealed class DuplicateMaterialDiagnostic(IDocumentQueryService documentQueries)
    : IElementDiagnostic
{
    private const string MaterialSignaturesQuery = "element-diagnostics:duplicate-material-signatures";

    public ElementDiagnosticId Identity => ElementDiagnosticIdCollector.DuplicateMaterial;

    public DiagnosticFeedback Execute(Document document, View? view, Element targetElement)
    {
        // The index is built once per document and lives in the transaction cache: without it
        // every target rescans the document and recomputes every signature (O(M^2)).
        var material = (Material)targetElement;
        MaterialSignatures signatures = GetSignatures(document);
        if (!signatures.SignaturesByMaterialId.TryGetValue(material.Id, out string? signature))
        {
            signature = ReadSignature(material);
            if (signature is null)
                return new(DiagnosticVerdict.Valid);
        }

        if (!signatures.CanonicalBySignature.TryGetValue(signature, out ElementId? canonicalId)
            || canonicalId == material.Id)
            return new(DiagnosticVerdict.Valid);
        if (document.GetElement(canonicalId) is not Material canonical)
            return new(DiagnosticVerdict.Valid);
        return new(DiagnosticVerdict.NotValid,
            new Dictionary<string, object> { ["duplicateName"] = canonical.Name });
    }

    private MaterialSignatures GetSignatures(Document document)
        => documentQueries.GetOrCreate(
            DocumentQueryKey.Create(document, MaterialSignaturesQuery),
            () => BuildSignatures(document));

    private MaterialSignatures BuildSignatures(Document document)
    {
        IReadOnlyList<Material> materials = documentQueries.GetElementsOfClass<Material>(document);
        var signaturesByMaterialId = new Dictionary<ElementId, string>(materials.Count);
        var canonicalBySignature = new Dictionary<string, ElementId>(materials.Count);
        foreach (Material material in materials)
        {
            string? signature = ReadSignature(material);
            if (signature is null)
                continue;
            signaturesByMaterialId[material.Id] = signature;
            if (!canonicalBySignature.TryGetValue(signature, out ElementId? canonicalId)
                || material.Id.Value() < canonicalId.Value())
                canonicalBySignature[signature] = material.Id;
        }

        return new(signaturesByMaterialId, canonicalBySignature);
    }

    // The name is not part of the signature: Revit enforces unique material names, so comparing
    // them would hide every genuine duplicate.
    private static string? ReadSignature(Material material)
    {
        try
        {
            return string.Join(";",
                $"class={material.MaterialClass ?? string.Empty}",
                $"category={material.MaterialCategory ?? string.Empty}",
                $"color={ReadColor(material.Color)}",
                $"transparency={material.Transparency.ToString(CultureInfo.InvariantCulture)}",
                $"shininess={material.Shininess.ToString(CultureInfo.InvariantCulture)}",
                $"smoothness={material.Smoothness.ToString(CultureInfo.InvariantCulture)}",
                $"surface-foreground={ReadPattern(material.SurfaceForegroundPatternId, material.SurfaceForegroundPatternColor)}",
                $"surface-background={ReadPattern(material.SurfaceBackgroundPatternId, material.SurfaceBackgroundPatternColor)}",
                $"cut-foreground={ReadPattern(material.CutForegroundPatternId, material.CutForegroundPatternColor)}",
                $"cut-background={ReadPattern(material.CutBackgroundPatternId, material.CutBackgroundPatternColor)}",
                $"appearance={ReadId(material.AppearanceAssetId)}",
                $"structural={ReadId(material.StructuralAssetId)}",
                $"thermal={ReadId(material.ThermalAssetId)}");
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            // A material whose settings cannot be read has no trustworthy signature, so it counts
            // as clean rather than failing the whole run.
            return null;
        }
    }

    private static string ReadColor(Color? color) => color is null
        ? "null"
        : $"{color.Red},{color.Green},{color.Blue}";

    private static string ReadPattern(ElementId? patternId, Color? color) => patternId is null
        ? "null"
        : $"{ReadId(patternId)}:{ReadColor(color)}";

    private static string ReadId(ElementId? id) => id is null
        ? "null"
        : id.Value().ToString(CultureInfo.InvariantCulture);

    private sealed record MaterialSignatures(
        Dictionary<ElementId, string> SignaturesByMaterialId,
        Dictionary<string, ElementId> CanonicalBySignature);
}
