using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Evaluates one Revit element and returns its diagnostic verdict.</summary>
public interface IElementDiagnostic
{
    /// <summary>Gets the identity that describes this diagnostic.</summary>
    ElementDiagnosticId Identity { get; }
    /// <summary>Evaluates an element in its document and optional view context.</summary>
    /// <param name="document">The document that owns the element.</param>
    /// <param name="view">The view that scoped the run, or <see langword="null"/> for document-wide execution.</param>
    /// <param name="targetElement">The element to evaluate.</param>
    /// <returns>The verdict and presentation data for the element.</returns>
    DiagnosticFeedback Execute(Document document, View? view, Element targetElement);
}
