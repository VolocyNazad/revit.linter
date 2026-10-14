using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementVisualization.Abstractions.Models;

/// <summary>Represents one typed operation in a visualization pipeline.</summary>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation; an empty list selects all sets.</param>
public abstract record ElementVisualizationStep(IReadOnlyList<string> ElementSetKeys);

/// <summary>Runs an element interaction operation such as show, select, isolate, or cut.</summary>
/// <param name="Type">The interaction operation to run.</param>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation.</param>
public sealed record AccentElementsStep(
    AccentElementsType Type,
    IReadOnlyList<string> ElementSetKeys)
    : ElementVisualizationStep(ElementSetKeys);

/// <summary>Overrides graphics individually for every element in the selected sets.</summary>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation.</param>
/// <param name="Style">The graphics settings to merge with the current element overrides.</param>
public sealed record OverrideElementsStep(
    IReadOnlyList<string> ElementSetKeys,
    ViewGraphicsStyle Style)
    : ElementVisualizationStep(ElementSetKeys);

/// <summary>Overrides graphics through a temporary selection filter.</summary>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation.</param>
/// <param name="Style">The graphics settings assigned to the temporary filter.</param>
public sealed record OverrideFilterStep(
    IReadOnlyList<string> ElementSetKeys,
    ViewGraphicsStyle Style)
    : ElementVisualizationStep(ElementSetKeys);

/// <summary>Sets the view detail level for the whole visualization view.</summary>
/// <param name="Level">The detail level to apply.</param>
/// <param name="ElementSetKeys">Kept empty: a view step consumes no element sets.</param>
public sealed record DetailLevelStep(
    ViewDetailLevel Level,
    IReadOnlyList<string> ElementSetKeys)
    : ElementVisualizationStep(ElementSetKeys);

/// <summary>Sets the view display style for the whole visualization view.</summary>
/// <param name="Style">The display style to apply.</param>
/// <param name="ElementSetKeys">Kept empty: a view step consumes no element sets.</param>
public sealed record DisplayStyleStep(
    DisplayStyle Style,
    IReadOnlyList<string> ElementSetKeys)
    : ElementVisualizationStep(ElementSetKeys);

/// <summary>Leaves only the given categories visible through the persistent view visibility.</summary>
/// <param name="Categories">The categories that stay visible; every other hideable category is hidden.</param>
/// <param name="ElementSetKeys">Kept empty: a category step consumes no element sets.</param>
public sealed record SetCategoryVisibilityStep(
    IReadOnlyList<BuiltInCategory> Categories,
    IReadOnlyList<string> ElementSetKeys)
    : ElementVisualizationStep(ElementSetKeys);
