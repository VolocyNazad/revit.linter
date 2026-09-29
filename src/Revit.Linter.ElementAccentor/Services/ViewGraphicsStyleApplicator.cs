using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Services;

internal static class ViewGraphicsStyleApplicator
{
    public static void Apply(Document document, OverrideGraphicSettings graphics, ViewGraphicsStyle style)
    {
        if (style.Halftone is bool halftone) graphics.SetHalftone(halftone);
        if (style.Transparency is int transparency) graphics.SetSurfaceTransparency(transparency);
        if (style.DetailLevel is ViewDetailLevel detailLevel) graphics.SetDetailLevel(detailLevel);

        ApplyLines(document, style.ProjectionLines, graphics.SetProjectionLineColor,
            graphics.SetProjectionLinePatternId, graphics.SetProjectionLineWeight);
        ApplyLines(document, style.CutLines, graphics.SetCutLineColor,
            graphics.SetCutLinePatternId, graphics.SetCutLineWeight);
        ApplyPattern(document, style.SurfaceForeground, graphics.SetSurfaceForegroundPatternColor,
            graphics.SetSurfaceForegroundPatternId, graphics.SetSurfaceForegroundPatternVisible);
        ApplyPattern(document, style.SurfaceBackground, graphics.SetSurfaceBackgroundPatternColor,
            graphics.SetSurfaceBackgroundPatternId, graphics.SetSurfaceBackgroundPatternVisible);
        ApplyPattern(document, style.CutForeground, graphics.SetCutForegroundPatternColor,
            graphics.SetCutForegroundPatternId, graphics.SetCutForegroundPatternVisible);
        ApplyPattern(document, style.CutBackground, graphics.SetCutBackgroundPatternColor,
            graphics.SetCutBackgroundPatternId, graphics.SetCutBackgroundPatternVisible);
    }

    private static void ApplyLines(
        Document document,
        LineGraphicsStyle? style,
        Func<Color, OverrideGraphicSettings> setColor,
        Func<ElementId, OverrideGraphicSettings> setPattern,
        Func<int, OverrideGraphicSettings> setWeight)
    {
        if (style is null) return;
        if (style.Color is not null) setColor(style.Color);
        if (style.PatternId is not null) setPattern(style.PatternId);
        else if (style.PatternName is not null) setPattern(ResolveLinePattern(document, style.PatternName));
        if (style.Weight is int weight) setWeight(weight);
    }

    private static void ApplyPattern(
        Document document,
        PatternGraphicsStyle? style,
        Func<Color, OverrideGraphicSettings> setColor,
        Func<ElementId, OverrideGraphicSettings> setPattern,
        Func<bool, OverrideGraphicSettings> setVisible)
    {
        if (style is null) return;
        if (style.Color is not null) setColor(style.Color);
        if (style.PatternId is not null) setPattern(style.PatternId);
        else if (style.PatternName is not null) setPattern(ResolveFillPattern(document, style.PatternName));
        if (style.IsVisible is bool isVisible) setVisible(isVisible);
    }

    private static ElementId ResolveLinePattern(Document document, string name)
    {
        if (string.Equals(name, "Solid", StringComparison.OrdinalIgnoreCase))
            return ElementId.InvalidElementId;

        return new FilteredElementCollector(document)
            .OfClass(typeof(LinePatternElement))
            .Cast<LinePatternElement>()
            .FirstOrDefault(pattern => string.Equals(pattern.Name, name, StringComparison.OrdinalIgnoreCase))?.Id
            ?? throw new InvalidOperationException($"Line pattern '{name}' was not found.");
    }

    private static ElementId ResolveFillPattern(Document document, string name)
    {
        FillPatternElement? pattern = new FilteredElementCollector(document)
            .OfClass(typeof(FillPatternElement))
            .Cast<FillPatternElement>()
            .FirstOrDefault(candidate =>
                string.Equals(name, "SolidFill", StringComparison.OrdinalIgnoreCase)
                    ? candidate.GetFillPattern().IsSolidFill
                    : string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
        return pattern?.Id ?? throw new InvalidOperationException($"Fill pattern '{name}' was not found.");
    }
}
