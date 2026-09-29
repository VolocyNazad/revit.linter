using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Determines whether an element diagnostic applies to a Revit document.</summary>
public interface IElementDiagnosticDocumentFilter
{
    /// <summary>Gets the identity of the diagnostic being filtered.</summary>
    ElementDiagnosticId Identity { get; }
    /// <summary>Determines whether the diagnostic should inspect elements in a document.</summary>
    /// <param name="document">The candidate document.</param>
    /// <returns><see langword="true"/> when the diagnostic applies.</returns>
    bool IsRelevantFor(Document document);
}
