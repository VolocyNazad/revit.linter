using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementVisualization.Abstractions.Models;

/// <summary>Represents one typed operation in a visualization pipeline.</summary>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation; an empty list selects all sets.</param>
public abstract record ElementVisualizationStep(IReadOnlyList<string> ElementSetKeys)
{
    /// <summary>
    /// Gets whether the step needs the visualization view active and visible, so it cannot run inside
    /// a transaction.
    /// </summary>
    /// <remarks>
    /// The pipeline commits the preceding steps and activates the view before such a step, because
    /// Revit rejects a change of the active view while the document is modifiable.
    /// </remarks>
    public virtual bool RequiresActiveView => false;

    /// <summary>Gets whether the step changes the document and therefore runs inside a transaction.</summary>
    public virtual bool RequiresTransaction => true;
}

/// <summary>Runs an element interaction operation such as show, select, isolate, or cut.</summary>
/// <param name="Type">The interaction operation to run.</param>
/// <param name="ElementSetKeys">Semantic element sets consumed by the operation.</param>
public sealed record AccentElementsStep(
    AccentElementsType Type,
    IReadOnlyList<string> ElementSetKeys)
    : ElementVisualizationStep(ElementSetKeys)
{
    /// <inheritdoc />
    public override bool RequiresActiveView => Type == AccentElementsType.ShowElements;

    /// <inheritdoc />
    public override bool RequiresTransaction =>
        Type is not (AccentElementsType.ShowElements or AccentElementsType.SelectElements);
}

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
