using Microsoft.Extensions.Logging;
using Revit.Linter.Core.Abstractions.Models;
using Revit.Linter.Core.Abstractions.Services;
using Revit.Linter.ElementAccentor.Abstractions.Models;
using Revit.Linter.ElementAccentor.Abstractions.Services;
using Revit.Linter.ElementVisualization.Abstractions.Models;
using Revit.Linter.ElementVisualization.Abstractions.Services;

namespace Revit.Linter.ElementVisualization.Services;

internal sealed class ElementVisualizationPipelineFactory(
    IEnumerable<IAccentElementsService> services,
    IOverrideElementGraphicsService overrideElementGraphicsService,
    IOverrideFilterGraphicsService overrideFilterGraphicsService,
    ISetCategoryVisibilityService setCategoryVisibilityService,
    IViewSettingsService viewSettingsService,
    ILogger<ElementVisualizationPipelineFactory> logger)
    : IElementVisualizationPipelineFactory
{
    private readonly IAccentElementsService[] _services = services.ToArray();

    public IElementVisualizationPipeline Create(
        ElementDiagnosticId identity,
        string name,
        IReadOnlyList<ElementVisualizationStepDefinition> steps)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Visualization pipeline name cannot be empty.", nameof(name));
        if (steps.Count == 0)
            throw new ArgumentException("Visualization pipeline must contain at least one step.", nameof(steps));

        ElementVisualizationStep[] parsedSteps = steps
            .Select(Parse)
            .ToArray();
        return new ConfiguredElementVisualizationPipeline(
            identity, name.Trim(), _services, overrideElementGraphicsService,
            overrideFilterGraphicsService, setCategoryVisibilityService, viewSettingsService, logger, parsedSteps);
    }

    private static ElementVisualizationStep Parse(ElementVisualizationStepDefinition step)
    {
        if (string.IsNullOrWhiteSpace(step.Type))
            throw new ArgumentException("Visualization step type cannot be empty.");

        string[] elementSets = step.ElementSets
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return step.Type.Trim().ToUpperInvariant() switch
        {
            "SHOW" or "SHOWELEMENTS" => Accent(AccentElementsType.ShowElements),
            "SELECT" or "SELECTELEMENTS" => Accent(AccentElementsType.SelectElements),
            "ISOLATE" or "ISOLATEELEMENTSONVIEW" => Accent(AccentElementsType.IsolateElementsOnView),
            "CUT" or "CUTVIEWBYELEMENTS" => Accent(AccentElementsType.CutViewByElements),
            "OVERRIDEELEMENTS" => new OverrideElementsStep(elementSets, ParseStyle(step)),
            "OVERRIDEFILTER" => new OverrideFilterStep(elementSets, ParseStyle(step)),
            "DETAILLEVEL" or "SETDETAILLEVEL" => new DetailLevelStep(ParseDetailLevel(step, elementSets), []),
            "DISPLAYSTYLE" or "SETDISPLAYSTYLE" => new DisplayStyleStep(ParseDisplayStyle(step, elementSets), []),
            "SETCATEGORYVISIBILITY" => new SetCategoryVisibilityStep(ParseCategories(step, elementSets), []),
            _ => throw new ArgumentException(
                $"Unknown visualization step '{step.Type}'. Supported values: Show, Select, Isolate, " +
                "Cut, OverrideElements, OverrideFilter, DetailLevel, DisplayStyle, SetCategoryVisibility.")
        };

        AccentElementsStep Accent(AccentElementsType type)
        {
            if (step.Style is not null)
                throw new ArgumentException($"Visualization step '{step.Type}' does not support style.");
            return new(type, elementSets);
        }
    }

    private static ViewGraphicsStyle ParseStyle(ElementVisualizationStepDefinition step)
    {
        ElementVisualizationGraphicsStyleDefinition source = step.Style
            ?? throw new ArgumentException($"Visualization step '{step.Type}' requires style.");
        if (source.Transparency is < 0 or > 100)
            throw new ArgumentException("Transparency must be between 0 and 100.");

        ViewDetailLevel? detailLevel = null;
        if (!string.IsNullOrWhiteSpace(source.DetailLevel))
        {
            if (!Enum.TryParse(source.DetailLevel, true, out ViewDetailLevel parsed))
                throw new ArgumentException($"Unknown view detail level '{source.DetailLevel}'.");
            detailLevel = parsed;
        }

        return new()
        {
            Halftone = source.Halftone,
            Transparency = source.Transparency,
            DetailLevel = detailLevel,
            ProjectionLines = ParseLines(source.ProjectionLines),
            CutLines = ParseLines(source.CutLines),
            SurfaceForeground = ParsePattern(source.SurfaceForeground),
            SurfaceBackground = ParsePattern(source.SurfaceBackground),
            CutForeground = ParsePattern(source.CutForeground),
            CutBackground = ParsePattern(source.CutBackground)
        };
    }

    private static ViewDetailLevel ParseDetailLevel(
        ElementVisualizationStepDefinition step, string[] elementSets)
    {
        RejectViewStepExtras(step, elementSets);
        if (string.IsNullOrWhiteSpace(step.DetailLevel))
            throw new ArgumentException($"Visualization step '{step.Type}' requires detailLevel.");
        if (!Enum.TryParse(step.DetailLevel, true, out ViewDetailLevel level))
            throw new ArgumentException($"Unknown view detail level '{step.DetailLevel}'.");
        if (level == ViewDetailLevel.Undefined)
            throw new ArgumentException("View detail level cannot be Undefined.");
        return level;
    }

    private static DisplayStyle ParseDisplayStyle(
        ElementVisualizationStepDefinition step, string[] elementSets)
    {
        RejectViewStepExtras(step, elementSets);
        if (string.IsNullOrWhiteSpace(step.DisplayStyle))
            throw new ArgumentException($"Visualization step '{step.Type}' requires displayStyle.");
        if (!Enum.TryParse(step.DisplayStyle, true, out DisplayStyle style))
            throw new ArgumentException($"Unknown display style '{step.DisplayStyle}'.");
        if (style == DisplayStyle.Undefined)
            throw new ArgumentException("Display style cannot be Undefined.");
        return style;
    }

    private static void RejectViewStepExtras(ElementVisualizationStepDefinition step, string[] elementSets)
    {
        if (step.Style is not null)
            throw new ArgumentException($"Visualization step '{step.Type}' does not support style.");
        if (elementSets.Length != 0)
            throw new ArgumentException($"Visualization step '{step.Type}' does not support element sets.");
        if (step.Categories.Length != 0)
            throw new ArgumentException($"Visualization step '{step.Type}' does not support categories.");
    }

    private static IReadOnlyList<BuiltInCategory> ParseCategories(
        ElementVisualizationStepDefinition step, string[] elementSets)
    {
        if (step.Style is not null)
            throw new ArgumentException($"Visualization step '{step.Type}' does not support style.");
        if (elementSets.Length != 0)
            throw new ArgumentException($"Visualization step '{step.Type}' does not support element sets.");
        string[] categories = step.Categories
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (categories.Length == 0)
            throw new ArgumentException($"Visualization step '{step.Type}' requires categories.");
        return categories
            .Select(name =>
            {
                if (!Enum.TryParse(name, true, out BuiltInCategory category)
                    || category == BuiltInCategory.INVALID)
                    throw new ArgumentException(
                        $"Unknown category '{name}'. Use a BuiltInCategory name such as 'OST_Walls'.");
                return category;
            })
            .ToArray();
    }

    private static LineGraphicsStyle? ParseLines(ElementVisualizationLineStyleDefinition? source)
    {
        if (source is null) return null;
        if (source.Weight is < 1 or > 16)
            throw new ArgumentOutOfRangeException(nameof(source), "Line weight must be between 1 and 16.");
        return new()
        {
            Color = ParseColor(source.Color),
            PatternName = Normalize(source.Pattern),
            Weight = source.Weight
        };
    }

    private static PatternGraphicsStyle? ParsePattern(ElementVisualizationPatternStyleDefinition? source) =>
        source is null
            ? null
            : new()
            {
                Color = ParseColor(source.Color),
                PatternName = Normalize(source.Pattern),
                IsVisible = source.IsVisible
            };

    private static Color? ParseColor(string? value)
    {
        if (value is null) return null;
        value = value.Trim();
        if (value.Length == 0) return null;
        string hex = value.TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out int rgb))
            throw new ArgumentException($"Color '{value}' must use #RRGGBB format.", nameof(value));
        return new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    private static string? Normalize(string? value)
    {
        if (value is null) return null;
        value = value.Trim();
        return value.Length == 0 ? null : value;
    }

    private sealed class ConfiguredElementVisualizationPipeline(
        ElementDiagnosticId identity,
        string value,
        IEnumerable<IAccentElementsService> services,
        IOverrideElementGraphicsService overrideElementGraphicsService,
        IOverrideFilterGraphicsService overrideFilterGraphicsService,
ISetCategoryVisibilityService setCategoryVisibilityService,
        IViewSettingsService viewSettingsService,
        ILogger logger,
        IReadOnlyList<ElementVisualizationStep> steps)
        : ElementVisualizationPipelineBase(
            identity, value, services, overrideElementGraphicsService, overrideFilterGraphicsService,
            setCategoryVisibilityService, viewSettingsService, logger)
    {
        protected override IReadOnlyList<ElementVisualizationStep> Steps { get; } = steps;
    }
}
