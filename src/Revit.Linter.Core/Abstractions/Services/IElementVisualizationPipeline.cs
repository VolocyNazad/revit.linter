using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Applies a diagnostic visualization and restores the state that preceded it.
/// </summary>
/// <remarks>
/// A pipeline instance owns the state captured by its latest successful or partially successful
/// application. Implementations must restore completed steps in reverse order when application
/// fails, when <see cref="Restore"/> is called, or when the pipeline is disposed.
/// </remarks>
public interface IElementVisualizationPipeline : IDisposable
{
    /// <summary>
    /// Gets the diagnostic for which the pipeline is available.
    /// </summary>
    ElementDiagnosticId Identity { get; }

    /// <summary>
    /// Gets the user-facing pipeline name.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Applies the configured steps to the supplied visualization context.
    /// </summary>
    /// <param name="context">The document, view, and element sets to visualize.</param>
    /// <returns><see langword="true"/> when every step was applied; otherwise, <see langword="false"/>.</returns>
    bool Apply(ElementVisualizationContext context);

    /// <summary>
    /// Restores the state captured by the latest application and clears the captured state.
    /// </summary>
    void Restore();
}
