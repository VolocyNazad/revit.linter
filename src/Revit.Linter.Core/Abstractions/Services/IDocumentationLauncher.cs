using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Opens pages of the user documentation for the user.</summary>
public interface IDocumentationLauncher
{
    /// <summary>Opens the page in the default browser, in the language of the current UI culture.</summary>
    /// <param name="page">The page to open.</param>
    /// <remarks>
    /// A failure to start the browser is logged and reported to the user by the implementation; it is not
    /// thrown to the caller.
    /// </remarks>
    void Open(DocumentationPage page);
}
