namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>
/// Describes a named visualization pipeline declared in diagnostic configuration.
/// </summary>
public sealed record ElementVisualizationPipelineDefinition
{
    /// <summary>
    /// Gets the name shown to the user when choosing a visualization.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the ordered visualization steps executed by the pipeline.
    /// </summary>
    public required ElementVisualizationStepDefinition[] Steps { get; init; }
}

/// <summary>Defines one ordered visualization operation in diagnostic configuration.</summary>
public sealed record ElementVisualizationStepDefinition
{
    /// <summary>Gets the registered step type.</summary>
    public required string Type { get; init; }

    /// <summary>Gets semantic element set keys; an empty array selects all sets.</summary>
    public string[] ElementSets { get; init; } = [];

    /// <summary>Gets graphics settings used by an override step.</summary>
    public ElementVisualizationGraphicsStyleDefinition? Style { get; init; }
}

/// <summary>Defines view graphics overrides in diagnostic configuration.</summary>
public sealed record ElementVisualizationGraphicsStyleDefinition
{
    /// <summary>Gets whether the elements are displayed in halftone.</summary>
    public bool? Halftone { get; init; }
    /// <summary>Gets transparency from 0 to 100.</summary>
    public int? Transparency { get; init; }
    /// <summary>Gets a <c>ViewDetailLevel</c> name.</summary>
    public string? DetailLevel { get; init; }
    /// <summary>Gets projection line settings.</summary>
    public ElementVisualizationLineStyleDefinition? ProjectionLines { get; init; }
    /// <summary>Gets cut line settings.</summary>
    public ElementVisualizationLineStyleDefinition? CutLines { get; init; }
    /// <summary>Gets surface foreground pattern settings.</summary>
    public ElementVisualizationPatternStyleDefinition? SurfaceForeground { get; init; }
    /// <summary>Gets surface background pattern settings.</summary>
    public ElementVisualizationPatternStyleDefinition? SurfaceBackground { get; init; }
    /// <summary>Gets cut foreground pattern settings.</summary>
    public ElementVisualizationPatternStyleDefinition? CutForeground { get; init; }
    /// <summary>Gets cut background pattern settings.</summary>
    public ElementVisualizationPatternStyleDefinition? CutBackground { get; init; }
}

/// <summary>Defines line graphics in diagnostic configuration.</summary>
public sealed record ElementVisualizationLineStyleDefinition
{
    /// <summary>Gets an RGB color in <c>#RRGGBB</c> format.</summary>
    public string? Color { get; init; }
    /// <summary>Gets a line pattern name from the target document.</summary>
    public string? Pattern { get; init; }
    /// <summary>Gets the line weight from 1 to 16.</summary>
    public int? Weight { get; init; }
}

/// <summary>Defines fill pattern graphics in diagnostic configuration.</summary>
public sealed record ElementVisualizationPatternStyleDefinition
{
    /// <summary>Gets an RGB color in <c>#RRGGBB</c> format.</summary>
    public string? Color { get; init; }
    /// <summary>Gets a fill pattern name from the target document.</summary>
    public string? Pattern { get; init; }
    /// <summary>Gets whether the pattern is visible.</summary>
    public bool? IsVisible { get; init; }
}
