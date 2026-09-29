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
