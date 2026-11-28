using Autodesk.Revit.DB;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Answers whether a document fix can be offered for a document.</summary>
public interface IDocumentFixAvailability
{
    /// <summary>Gets a value indicating whether the fix applies to the document.</summary>
    /// <param name="targetDocument">The document the report belongs to.</param>
    /// <returns><see langword="true"/> when the fix can be offered.</returns>
    bool IsAvailable(Document targetDocument);
}
