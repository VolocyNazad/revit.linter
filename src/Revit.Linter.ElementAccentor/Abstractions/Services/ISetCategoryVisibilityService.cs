namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Hides every hideable top-level category except the given ones through the persistent
/// view visibility, so only the listed categories stay visible.
/// </summary>
/// <remarks>
/// Categories absent from the document are ignored: one configuration runs across documents
/// with different category sets. Restoring unhides exactly the categories hidden by
/// <see cref="Apply"/>, leaving categories hidden beforehand hidden. Unlike temporary
/// isolation, this operation composes with an already active temporary hide/isolate state.
/// </remarks>
public interface ISetCategoryVisibilityService
{
    /// <summary>Applies the category visibility and captures the state required to undo it.</summary>
    /// <param name="document">The Revit document that owns the categories.</param>
    /// <param name="view">The view affected by the operation.</param>
    /// <param name="categories">The categories that stay visible.</param>
    /// <returns>A session that restores the preceding category visibility.</returns>
    IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyList<BuiltInCategory> categories);
}
