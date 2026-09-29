using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Applies a user-selectable correction for a document diagnostic finding.</summary>
public interface IDocumentFix
{
    /// <summary>Gets the identity of the diagnostic corrected by this fix.</summary>
    DocumentDiagnosticId Identity { get; }
    /// <summary>Gets the user-facing fix name.</summary>
    string Value { get; }
    /// <summary>Attempts to apply the fix to a document.</summary>
    /// <param name="targetDocument">The document to modify.</param>
    /// <returns><see langword="true"/> when the fix was applied.</returns>
    bool Execute(Document targetDocument);
}
