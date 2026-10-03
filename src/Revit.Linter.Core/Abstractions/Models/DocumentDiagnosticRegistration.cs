using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Groups the runtime components registered for one document diagnostic.</summary>
/// <param name="Identity">The stable diagnostic identity.</param>
/// <param name="Diagnostic">The diagnostic implementation.</param>
/// <param name="Filter">The document applicability filter.</param>
/// <param name="Override">The effective user settings.</param>
/// <param name="Fixes">The fixes offered for findings.</param>
public sealed record DocumentDiagnosticRegistration(
    DocumentDiagnosticId Identity,
    IDocumentDiagnostic Diagnostic,
    IDocumentDiagnosticFilter Filter,
    DocumentDiagnosticIdOverride Override,
    IReadOnlyList<IDocumentFix> Fixes)
{
    /// <summary>
    /// Gets the documentation page that describes the diagnostic, or <see langword="null"/> when the
    /// registering module does not provide one.
    /// </summary>
    public DocumentationPage? Documentation { get; init; }
}
