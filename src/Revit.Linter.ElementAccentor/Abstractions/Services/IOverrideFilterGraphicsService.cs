using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>Applies reversible view graphics through a temporary selection filter.</summary>
public interface IOverrideFilterGraphicsService
{
    /// <summary>Creates a temporary filter, applies its style, and captures cleanup as a session.</summary>
    /// <param name="document">The Revit document that owns the elements.</param>
    /// <param name="view">The view receiving the filter.</param>
    /// <param name="elementIds">Elements included in the temporary selection filter.</param>
    /// <param name="style">Graphics properties assigned to the filter.</param>
    /// <returns>A session that removes the temporary filter and its overrides.</returns>
    IElementAccentSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds,
        ViewGraphicsStyle style);
}
