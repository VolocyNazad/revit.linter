using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Applies a user-selectable correction for an element diagnostic finding.</summary>
public interface IElementFix
{
    /// <summary>Gets the identity of the diagnostic corrected by this fix.</summary>
    ElementDiagnosticId Identity { get; }
    /// <summary>Gets the user-facing fix name.</summary>
    string Value { get; }
    /// <summary>Attempts to apply the fix to an element.</summary>
    /// <param name="targetElement">The element to modify.</param>
    /// <returns><see langword="true"/> when the fix was applied.</returns>
    bool Execute(Element targetElement);
}
