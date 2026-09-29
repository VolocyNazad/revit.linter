using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>Applies reversible per-element view graphics overrides.</summary>
public interface IOverrideElementGraphicsService
{
    /// <summary>Applies a style and captures the preceding overrides for restoration.</summary>
    /// <param name="document">The Revit document that owns the elements.</param>
    /// <param name="view">The view whose graphics are changed.</param>
    /// <param name="elementIds">Elements whose graphics are overridden.</param>
    /// <param name="style">Graphics properties to merge into the existing overrides.</param>
    /// <returns>A session that restores every previous element override.</returns>
    IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds,
        ViewGraphicsStyle style);
}
