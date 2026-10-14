using Revit.Linter.ElementAccentor.Abstractions.Models;

namespace Revit.Linter.ElementAccentor.Abstractions.Services;

/// <summary>Performs one atomic interaction with a set of Revit elements.</summary>
public interface IAccentElementsService
{
    /// <summary>Gets the operation implemented by the service.</summary>
    AccentElementsType Type { get; }

    /// <summary>Runs the operation once against the active view without retaining rollback state.</summary>
    /// <param name="document">The active Revit document.</param>
    /// <param name="elementIds">Elements participating in the operation.</param>
    /// <returns><see langword="true"/> when the operation was applied.</returns>
    /// <remarks>For one-shot interface commands; visualization pipelines use <see cref="Apply"/> instead.</remarks>
    bool RunOnce(Document document, params ElementId[] elementIds);

    /// <summary>Applies the operation and captures the state required to undo it.</summary>
    /// <param name="document">The Revit document that owns the elements.</param>
    /// <param name="view">The view affected by the operation.</param>
    /// <param name="elementIds">Elements participating in the operation.</param>
    /// <returns>A session that restores the preceding UI or view state.</returns>
    /// <remarks>For visualization pipeline steps; one-shot interface commands use <see cref="RunOnce"/> instead.</remarks>
    IRestoreSession Apply(
        Document document,
        View view,
        IReadOnlyCollection<ElementId> elementIds);
}
