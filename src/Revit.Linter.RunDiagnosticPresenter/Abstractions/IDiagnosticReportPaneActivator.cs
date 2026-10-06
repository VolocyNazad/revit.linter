namespace Revit.Linter.RunDiagnosticPresenter.Abstractions;

/// <summary>
/// Brings the diagnostic report pane to the foreground for the current Revit session.
/// </summary>
public interface IDiagnosticReportPaneActivator
{
    /// <summary>
    /// Shows or activates the diagnostic report pane.
    /// </summary>
    void Activate();
}
