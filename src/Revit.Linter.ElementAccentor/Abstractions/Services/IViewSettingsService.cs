namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>
/// Temporarily changes whole-view settings and restores them afterwards.
/// </summary>
/// <remarks>
/// View settings are document modifications: every change runs in its own transaction,
/// like any other accent operation. A setting that already matches is a no-op session.
/// </remarks>
public interface IViewSettingsService
{
    /// <summary>Applies the view detail level and captures the state required to undo it.</summary>
    /// <param name="document">The Revit document that owns the view.</param>
    /// <param name="view">The view affected by the operation.</param>
    /// <param name="level">The detail level to apply.</param>
    /// <returns>A session that restores the preceding detail level.</returns>
    IRestoreSession ApplyDetailLevel(Document document, View view, ViewDetailLevel level);

    /// <summary>Applies the view display style and captures the state required to undo it.</summary>
    /// <param name="document">The Revit document that owns the view.</param>
    /// <param name="view">The view affected by the operation.</param>
    /// <param name="style">The display style to apply.</param>
    /// <returns>A session that restores the preceding display style.</returns>
    IRestoreSession ApplyDisplayStyle(Document document, View view, DisplayStyle style);
}
