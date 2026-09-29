using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Determines whether an element diagnostic applies to an individual element.</summary>
public interface IElementDiagnosticFilter
{
    /// <summary>Gets the identity of the diagnostic being filtered.</summary>
    ElementDiagnosticId Identity { get; }
    /// <summary>Determines whether the diagnostic should evaluate an element.</summary>
    /// <param name="document">The document that owns the element.</param>
    /// <param name="element">The candidate element.</param>
    /// <returns><see langword="true"/> when the diagnostic applies.</returns>
    bool IsRelevantFor(Document document, Element element);
}
