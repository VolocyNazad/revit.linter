namespace Revit.Linter.Diagnostic.Abstractions.Services;

/// <summary>
/// Executes registered diagnostics and publishes reports for invalid results.
/// </summary>
/// <remarks>
/// A diagnostic that throws does not stop the others. Its failure is logged once and published as an
/// <see cref="DiagnosticSeverity.Error"/> report under the diagnostic's code with the document as target;
/// an element diagnostic stops at its first failure. The operation then returns
/// <see cref="DiagnosticServiceResult.Failed"/> although the reports of the remaining diagnostics are complete.
/// </remarks>
public interface IDiagnosticService
{
    /// <summary>
    /// Executes document diagnostics and element diagnostics for the specified elements.
    /// </summary>
    /// <param name="document">The document to diagnose.</param>
    /// <param name="elementIds">The identifiers of elements to diagnose.</param>
    /// <param name="view">The optional view context supplied to element diagnostics.</param>
    /// <returns>The outcome of the diagnostic operation.</returns>
    DiagnosticServiceResult Execute(Document document, IEnumerable<ElementId> elementIds, View? view = null);

    /// <summary>
    /// Executes document diagnostics and element diagnostics for elements collected from the document or view.
    /// </summary>
    /// <param name="document">The document to diagnose.</param>
    /// <param name="view">The optional view that limits element collection and supplies diagnostic context.</param>
    /// <returns>The outcome of the diagnostic operation.</returns>
    DiagnosticServiceResult Execute(Document document, View? view = null);
}
