using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Evaluates a Revit document and produces zero or more findings.</summary>
public interface IDocumentDiagnostic
{
    /// <summary>Gets the identity that describes this diagnostic.</summary>
    DocumentDiagnosticId Identity { get; }
    /// <summary>Evaluates the supplied document.</summary>
    /// <param name="targetDocument">The document to evaluate.</param>
    /// <returns>The findings produced by the diagnostic.</returns>
    IEnumerable<DiagnosticFeedback> Execute(Document targetDocument);
}
