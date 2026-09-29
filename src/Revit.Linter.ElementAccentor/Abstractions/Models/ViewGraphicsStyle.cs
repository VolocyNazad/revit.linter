namespace Revit.Linter.ElementAccentor.Abstractions.Models;

/// <summary>Describes view-specific graphics overrides for a group of elements.</summary>
/// <remarks>A <see langword="null"/> property leaves the corresponding setting unchanged.</remarks>
public sealed record ViewGraphicsStyle
{
    /// <summary>Gets whether the elements are displayed in halftone.</summary>
    public bool? Halftone { get; init; }

    /// <summary>Gets the surface transparency from 0 (opaque) to 100 (fully transparent).</summary>
    public int? Transparency { get; init; }

    /// <summary>Gets the detail level forced for the elements.</summary>
    public ViewDetailLevel? DetailLevel { get; init; }

    /// <summary>Gets projection line overrides.</summary>
    public LineGraphicsStyle? ProjectionLines { get; init; }

    /// <summary>Gets cut line overrides.</summary>
    public LineGraphicsStyle? CutLines { get; init; }

    /// <summary>Gets surface foreground pattern overrides.</summary>
    public PatternGraphicsStyle? SurfaceForeground { get; init; }

    /// <summary>Gets surface background pattern overrides.</summary>
    public PatternGraphicsStyle? SurfaceBackground { get; init; }

    /// <summary>Gets cut foreground pattern overrides.</summary>
    public PatternGraphicsStyle? CutForeground { get; init; }

    /// <summary>Gets cut background pattern overrides.</summary>
    public PatternGraphicsStyle? CutBackground { get; init; }
}

/// <summary>Describes overrides for a projection or cut line.</summary>
public sealed record LineGraphicsStyle
{
    /// <summary>Gets the line color.</summary>
    public Color? Color { get; init; }

    /// <summary>Gets the exact line pattern element identifier.</summary>
    /// <remarks>Takes precedence over <see cref="PatternName"/>.</remarks>
    public ElementId? PatternId { get; init; }

    /// <summary>Gets the line pattern name resolved in the target document.</summary>
    public string? PatternName { get; init; }

    /// <summary>Gets the line weight from 1 to 16.</summary>
    public int? Weight { get; init; }
}

/// <summary>Describes overrides for a surface or cut fill pattern.</summary>
public sealed record PatternGraphicsStyle
{
    /// <summary>Gets the pattern color.</summary>
    public Color? Color { get; init; }

    /// <summary>Gets the exact fill pattern element identifier.</summary>
    /// <remarks>Takes precedence over <see cref="PatternName"/>.</remarks>
    public ElementId? PatternId { get; init; }

    /// <summary>Gets the fill pattern name resolved in the target document.</summary>
    public string? PatternName { get; init; }

    /// <summary>Gets whether the pattern is visible.</summary>
    public bool? IsVisible { get; init; }
}
