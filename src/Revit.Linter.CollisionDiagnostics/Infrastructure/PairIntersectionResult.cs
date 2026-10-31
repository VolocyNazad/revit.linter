namespace Revit.Linter.CollisionDiagnostics.Infrastructure;

/// <summary>Describes what the solid check of an element pair established.</summary>
internal enum PairIntersectionResult
{
    /// <summary>The solids were compared and do not overlap.</summary>
    None,

    /// <summary>The solids were compared and overlap by more than the intersection tolerance.</summary>
    Intersects,

    /// <summary>
    /// Revit could not compute the intersection of the solids, so whether they overlap is unknown.
    /// </summary>
    /// <remarks>
    /// A rejected Boolean operation carries no information about an overlap: Revit also rejects it for
    /// coincident faces and edges, which is exactly the geometry of elements that only touch.
    /// </remarks>
    Unverified,
}
