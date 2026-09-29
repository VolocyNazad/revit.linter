using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>
/// Creates visualization pipelines from configuration definitions.
/// </summary>
public interface IElementVisualizationPipelineFactory
{
    /// <summary>
    /// Creates a pipeline for a diagnostic.
    /// </summary>
    /// <param name="identity">The diagnostic identity associated with the pipeline.</param>
    /// <param name="name">The user-facing pipeline name.</param>
    /// <param name="steps">The ordered step definitions to execute.</param>
    /// <returns>A pipeline instance that owns its applied visualization state.</returns>
    IElementVisualizationPipeline Create(
        ElementDiagnosticId identity,
        string name,
        IReadOnlyList<ElementVisualizationStepDefinition> steps);
}
