using Revit.Linter.Core.Abstractions.Models;

namespace Revit.Linter.Core.Abstractions.Services;

/// <summary>Creates element fixes from configuration pipeline definitions.</summary>
public interface IElementFixPipelineFactory
{
    /// <summary>Creates a fix pipeline for a diagnostic.</summary>
    /// <param name="identity">The diagnostic identity associated with the pipeline.</param>
    /// <param name="name">The user-facing pipeline name.</param>
    /// <param name="steps">The ordered step definitions to execute.</param>
    /// <returns>A fix that executes the configured steps.</returns>
    IElementFix Create(
        ElementDiagnosticId identity,
        string name,
        IReadOnlyList<ElementFixStepDefinition> steps);
}
