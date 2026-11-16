using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Colours the diagnostic targets of a document in the three-dimensional visualization view.
/// </summary>
public interface IElementHeatmapService
{
    /// <summary>Colours the supplied elements in the view and captures the previous graphics.</summary>
    /// <param name="document">The document that owns the elements.</param>
    /// <param name="view">The three-dimensional view that receives the colour.</param>
    /// <param name="entries">The elements and their heat map values.</param>
    /// <returns>The applied heat map and its restoration state.</returns>
    /// <remarks>
    /// The call runs in a Revit API context and changes the document. Elements without model geometry,
    /// elements that the view does not display and elements of a non-model category are skipped and
    /// reported by the returned session instead of failing the call.
    /// </remarks>
    IElementHeatmapSession Apply(
        Document document,
        View view,
        IReadOnlyList<FindingHeatmapEntry> entries);
}

/// <summary>Owns the colour applied by the findings heat map and restores the previous graphics.</summary>
public interface IElementHeatmapSession : IDisposable
{
    /// <summary>Gets the number of elements that received a heat map colour.</summary>
    int ColoredElementCount { get; }

    /// <summary>Gets the number of elements that had no model geometry or were not displayed in the view.</summary>
    int SkippedElementCount { get; }

    /// <summary>Restores the graphics overrides that preceded the heat map.</summary>
    /// <remarks>The call runs in a Revit API context. A view that was deleted or undone counts as restored.</remarks>
    void Restore();
}
