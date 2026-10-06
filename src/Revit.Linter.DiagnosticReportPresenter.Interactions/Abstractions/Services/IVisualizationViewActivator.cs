namespace Revit.Linter.DiagnosticReportPresenter.Interactions.Abstractions.Services;

/// <summary>
/// Prepares and activates the view used by diagnostic visualization pipelines.
/// </summary>
public interface IVisualizationViewActivator
{
    /// <summary>
    /// Prepares and activates a three-dimensional view suitable for diagnostic visualization.
    /// </summary>
    /// <remarks>
    /// The call must run in a Revit API context. It creates and names a view in a transaction when the
    /// active project document does not yet contain one, so the document can become modified. The view is
    /// activated only after that transaction has finished.
    /// </remarks>
    void Activate();
}
