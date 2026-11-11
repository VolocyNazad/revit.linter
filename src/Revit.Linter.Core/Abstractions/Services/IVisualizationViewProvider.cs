using Autodesk.Revit.DB;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Provides the three-dimensional view used by diagnostic visualization pipelines and activates the
/// view a visualization runs in.
/// </summary>
public interface IVisualizationViewProvider
{
    /// <summary>
    /// Returns the three-dimensional view used by diagnostic visualization, creating and preparing it
    /// when the active project document does not contain one.
    /// </summary>
    /// <remarks>
    /// The call must run in a Revit API context. The returned view is not activated: the pipeline
    /// calls <see cref="Activate"/> at the point its declared steps need the view on screen.
    /// </remarks>
    View3D GetOrCreateView();

    /// <summary>Shows the view as the active view of the active document.</summary>
    /// <param name="view">
    /// The view to show: the visualization view returned by <see cref="GetOrCreateView"/>, or the view
    /// that is the finding target when the finding itself is a view.
    /// </param>
    /// <remarks>
    /// The call must run in a Revit API context, and it must not run inside a transaction: Revit
    /// rejects a change of the active view while the document is modifiable. The pipeline calls it
    /// between two committed transactions for exactly that reason.
    /// </remarks>
    void Activate(View view);
}
