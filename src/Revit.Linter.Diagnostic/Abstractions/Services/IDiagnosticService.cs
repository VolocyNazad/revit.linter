namespace Revit.Linter.Diagnostic.Abstractions.Services;

/// <summary>
/// Executes registered diagnostics and publishes reports for invalid results.
/// </summary>
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
