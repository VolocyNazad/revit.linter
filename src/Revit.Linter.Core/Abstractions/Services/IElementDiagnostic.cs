using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Evaluates one Revit element and returns its diagnostic results.</summary>
public interface IElementDiagnostic
{
    /// <summary>Gets the identity that describes this diagnostic.</summary>
    ElementDiagnosticId Identity { get; }
    /// <summary>Evaluates an element in its document and optional view context.</summary>
    /// <param name="document">The document that owns the element.</param>
    /// <param name="view">The view that scoped the run, or <see langword="null"/> for document-wide execution.</param>
    /// <param name="targetElement">The element to evaluate.</param>
    /// <returns>
    /// The results for the element: one result per finding, in the order the check reports them, or a
    /// valid result when the element is clean. A check that finds several independent problems
    /// returns several invalid results, and every one of them becomes a row of the report.
    /// </returns>
    IReadOnlyList<DiagnosticFeedback> Execute(Document document, View? view, Element targetElement);
}
