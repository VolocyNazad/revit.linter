using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Applies an element fix to named target and dependency sets supplied by a diagnostic finding.
/// </summary>
public interface IElementSetFix : IElementFix
{
    /// <summary>Attempts to apply the fix to its configured element sets.</summary>
    /// <param name="context">The document and named element sets supplied by the diagnostic finding.</param>
    /// <returns><see langword="true"/> when the fix was applied.</returns>
    bool Execute(ElementFixContext context);
}
