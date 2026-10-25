namespace Revit.Linter.Core.Abstractions.Models;

/// <summary>Describes a named fix pipeline declared in diagnostic configuration.</summary>
public sealed record ElementFixPipelineDefinition
{
    /// <summary>Gets the name shown to the user when choosing a fix.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the ordered fix steps executed by the pipeline.</summary>
    public required ElementFixStepDefinition[] Steps { get; init; }
}

/// <summary>Defines one ordered fix operation in diagnostic configuration.</summary>
public sealed record ElementFixStepDefinition
{
    /// <summary>Gets the registered step type.</summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the named diagnostic element sets consumed by the step. An empty list selects only the target set.
    /// </summary>
    public string[] ElementSets { get; init; } = [];
}
