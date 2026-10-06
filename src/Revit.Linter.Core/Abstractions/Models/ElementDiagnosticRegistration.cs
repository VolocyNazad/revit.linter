using Revit.Linter.Core.Abstractions.Services;

namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Groups the runtime components registered for one element diagnostic.</summary>
/// <param name="Identity">The stable diagnostic identity.</param>
/// <param name="Diagnostic">The diagnostic implementation.</param>
/// <param name="Filter">The element applicability filter.</param>
/// <param name="DocumentFilter">The document applicability filter.</param>
/// <param name="Override">The effective user settings.</param>
/// <param name="Fixes">The fixes offered for findings.</param>
/// <param name="VisualizationPipelines">The visualizations offered for findings.</param>
/// <param name="ConfigurationPath">The configuration file the rule came from, or <see langword="null"/> for built-in diagnostics.</param>
public sealed record ElementDiagnosticRegistration(
    ElementDiagnosticId Identity,
    IElementDiagnostic Diagnostic,
    IElementDiagnosticFilter Filter,
    IElementDiagnosticDocumentFilter DocumentFilter,
    ElementDiagnosticIdOverride Override,
    IReadOnlyList<IElementFix> Fixes,
    IReadOnlyList<IElementVisualizationPipeline> VisualizationPipelines,
    string? ConfigurationPath = null)
{
    /// <summary>
    /// Gets the documentation page that describes the diagnostic, or <see langword="null"/> when the
    /// registering module does not provide one.
    /// </summary>
    public DocumentationPage? Documentation { get; init; }
}
