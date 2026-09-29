using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Determines whether a document diagnostic applies to a Revit document.</summary>
public interface IDocumentDiagnosticFilter
{
    /// <summary>Gets the identity of the diagnostic being filtered.</summary>
    DocumentDiagnosticId Identity { get; }
    /// <summary>Determines whether the diagnostic should run for a document.</summary>
    /// <param name="document">The candidate document.</param>
    /// <returns><see langword="true"/> when the diagnostic applies.</returns>
    bool IsRelevantFor(Document document);
}
