using Autodesk.Revit.DB;

namespace Revit.Linter.DiagnosticReportPresenter.Services;

/// <summary>
/// Describes one document to check in a scheduled diagnostic refresh.
/// </summary>
/// <param name="Document">The document to check.</param>
/// <param name="ElementIds">The elements to check again; empty when only the document findings refresh.</param>
/// <param name="IncludeDocumentDiagnostics">Whether the document diagnostics run with the element checks.</param>
internal sealed record DiagnosticRefreshBatch(
    Document Document,
    IReadOnlyCollection<ElementId> ElementIds,
    bool IncludeDocumentDiagnostics);

/// <summary>
/// Provides data for a scheduled diagnostic refresh.
/// </summary>
/// <param name="batches">The checks the report has to execute.</param>
internal sealed class DiagnosticRefreshRequestedEventArgs(IReadOnlyList<DiagnosticRefreshBatch> batches) : EventArgs
{
    /// <summary>Gets the checks the report has to execute.</summary>
    public IReadOnlyList<DiagnosticRefreshBatch> Batches { get; } = batches;
}
