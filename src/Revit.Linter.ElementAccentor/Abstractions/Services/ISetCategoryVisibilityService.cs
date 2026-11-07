using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Changes the persistent category visibility of a view for the listed categories and, in the
/// <see cref="CategoryVisibilityMode.OnlyVisible"/> and <see cref="CategoryVisibilityMode.OnlyHidden"/>
/// modes, for every other hideable category too.
/// </summary>
/// <remarks>
/// Categories absent from the document are ignored: one configuration runs across documents
/// with different category sets. Restoring restores the visibility every operation changed, including
/// categories that were hidden beforehand. Unlike temporary isolation, this operation composes with
/// an already active temporary hide/isolate state.
/// </remarks>
public interface ISetCategoryVisibilityService
{
    /// <summary>
    /// Changes the category visibility in the requested direction and captures the state required to
    /// undo the change.
    /// </summary>
    /// <param name="document">The Revit document that owns the categories.</param>
    /// <param name="view">The view affected by the operation.</param>
    /// <param name="categories">The listed categories.</param>
    /// <param name="mode">What happens to the listed categories and to the other hideable categories.</param>
    /// <returns>A session that restores the preceding category visibility.</returns>
    IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyList<BuiltInCategory> categories,
        CategoryVisibilityMode mode);
}
